using System.IO;
using System.Threading;
using System.Windows;
using HammamDesktop.Services;
using HammamDesktop.ViewModels;
using HammamDesktop.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace HammamDesktop;

/// <summary>
/// Application WPF principale
/// Configuration de l'injection de dépendances et démarrage
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;
    private static Mutex? _mutex;

    public App()
    {
        // Configuration Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/hammam-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 60)
            .WriteTo.Console()
            .CreateLogger();

        // Configuration Host avec DI
        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .Build();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Configuration
        services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
        services.Configure<SyncSettings>(configuration.GetSection("SyncSettings"));
        services.Configure<SessionSettings>(configuration.GetSection("SessionSettings"));

        // Base de données SQLite locale
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HammamPOS",
            "hammam_local.db"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        // Factory de DbContext : chaque opération crée son propre contexte.
        // Indispensable car la synchro en arrière-plan et l'UI accèdent à la base
        // en parallèle (DbContext n'est PAS thread-safe).
        services.AddDbContextFactory<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ITicketService, TicketService>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IPrintService, PrintService>();

        // HTTP Client principal avec Polly (retries courts : 5xx, 408, erreurs réseau)
        services.AddHttpClient("HammamApi", client =>
        {
            client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(
                int.Parse(configuration["ApiSettings:TimeoutSeconds"] ?? "30")
            );
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(2, retryAttempt =>
                TimeSpan.FromMilliseconds(400 * retryAttempt)
            )
        );

        // HTTP Client rapide sans retry (détection offline immédiate)
        services.AddHttpClient("HammamApiFast", client =>
        {
            client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SalesViewModel>();

        // Views
        services.AddTransient<Views.LoginWindow>();
        services.AddTransient<Views.MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Vérifier qu'une seule instance tourne
        const string mutexName = "HammamPOS_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show(
                "L'application Hammam POS est déjà en cours d'exécution.",
                "Hammam POS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try
        {
            Log.Information("=== Démarrage de l'application Hammam POS ===");

            await _host.StartAsync();

            await InitializeDatabaseAsync();

            // Démarrer le service de synchronisation
            var syncService = _host.Services.GetRequiredService<ISyncService>();
            syncService.StartBackgroundSync();

            // Afficher la fenêtre de login
            Log.Information("Affichage de la fenêtre de connexion");
            var loginWindow = _host.Services.GetRequiredService<Views.LoginWindow>();
            loginWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erreur fatale au démarrage de l'application");
            MessageBox.Show(
                $"Erreur au démarrage de l'application:\n\n{ex.Message}\n\nVeuillez consulter les logs pour plus de détails.",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown(1);
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        var dbFactory = _host.Services.GetRequiredService<IDbContextFactory<LocalDbContext>>();
        await using var dbContext = await dbFactory.CreateDbContextAsync();

        Log.Information("Initialisation de la base de données SQLite...");
        await dbContext.Database.EnsureCreatedAsync();

        // Créer la table employe_profiles si elle n'existe pas (pour les bases existantes)
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS employe_profiles (
                Id TEXT NOT NULL PRIMARY KEY,
                Username TEXT NOT NULL,
                Prenom TEXT,
                Nom TEXT,
                Icone TEXT,
                HammamId TEXT NOT NULL,
                HammamNom TEXT,
                HammamNomArabe TEXT,
                HammamPrefixeTicket INTEGER NOT NULL DEFAULT 100000,
                PasswordHash TEXT,
                EncryptedPassword TEXT NULL,
                ServerPasswordHash TEXT NULL,
                CachedAt TEXT NOT NULL
            )");

        // Colonnes ajoutées au fil des versions (bases existantes chez le client)
        await AddColumnIfMissingAsync(dbContext, "employe_profiles", "EncryptedPassword", "TEXT NULL");
        await AddColumnIfMissingAsync(dbContext, "employe_profiles", "ServerPasswordHash", "TEXT NULL");
        await AddColumnIfMissingAsync(dbContext, "tickets", "TicketNumber", "TEXT NULL");
        await AddColumnIfMissingAsync(dbContext, "tickets", "SyncAttempts", "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfMissingAsync(dbContext, "tickets", "LastSyncError", "TEXT NULL");

        // Anciens tickets sans numéro : numéro dérivé de l'Id COMPLET (unique).
        // Les versions précédentes n'utilisaient que 4 caractères de l'Id (65 536 valeurs) : deux tickets
        // pouvaient recevoir le même numéro et le serveur prenait le second pour un doublon (ticket perdu).
        // On renumérote donc aussi les tickets legacy pas encore synchronisés.
        await dbContext.Database.ExecuteSqlRawAsync(@"
            UPDATE tickets
            SET TicketNumber = 'LEGACY-' || upper(replace(Id, '-', ''))
            WHERE TicketNumber IS NULL OR TicketNumber = ''
               OR (SyncStatus = 'Pending' AND TicketNumber LIKE 'LEGACY-%' AND length(TicketNumber) < 39)
        ");

        // Profils invalides laissés par d'anciennes versions (cache de api/auth/profiles, Id vide)
        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM employe_profiles WHERE Id = '00000000-0000-0000-0000-000000000000'");

        // Unicité du numéro par hammam
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_tickets_hammam_ticket_number
                ON tickets(HammamId, TicketNumber)
                WHERE TicketNumber IS NOT NULL
            ");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Index d'unicité des numéros de ticket non créé (données legacy en conflit)");
        }

        Log.Information("Base de données initialisée avec succès");
    }

    private static async Task AddColumnIfMissingAsync(LocalDbContext db, string table, string column, string definition)
    {
        var exists = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_table_info({table}) WHERE name = {column}")
            .SingleAsync();

        if (exists == 0)
        {
            // ALTER TABLE n'accepte pas de paramètres SQL : noms internes constants uniquement
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition}");
#pragma warning restore EF1002
            Log.Information("Migration SQLite : colonne {Table}.{Column} ajoutée", table, column);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("=== Arrêt de l'application ===");

        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    public static T GetService<T>() where T : class
    {
        var app = (App)Current;
        return app._host.Services.GetRequiredService<T>();
    }
}

// Classes de configuration
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class SyncSettings
{
    public int IntervalMinutes { get; set; } = 1;
    public int BatchSize { get; set; } = 50;
    public int RetryCount { get; set; } = 3;
}

public class SessionSettings
{
    public int ExpirationHours { get; set; } = 8;
}
