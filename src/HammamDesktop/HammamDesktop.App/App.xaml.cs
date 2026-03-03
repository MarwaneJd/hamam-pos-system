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
            .WriteTo.File("logs/hammam-.log", rollingInterval: RollingInterval.Day)
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
        
        services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ITicketService, TicketService>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IPrintService, PrintService>();

        // HTTP Client avec Polly
        services.AddHttpClient("HammamApi", client =>
        {
            client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(
                int.Parse(configuration["ApiSettings:TimeoutSeconds"] ?? "30")
            );
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            )
        );

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

            // Initialiser la base de données
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                
                Log.Information("Initialisation de la base de données SQLite...");
                await dbContext.Database.EnsureCreatedAsync();
                Log.Information("Base de données initialisée avec succès");
            }

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
    public int IntervalMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
    public int RetryCount { get; set; } = 3;
}

public class SessionSettings
{
    public int ExpirationHours { get; set; } = 8;
}
