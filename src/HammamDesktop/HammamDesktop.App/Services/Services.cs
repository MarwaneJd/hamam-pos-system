using HammamDesktop.Data;
using HammamDesktop.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace HammamDesktop.Services;

#region Interfaces

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<bool> HasValidSessionAsync();
    Task<LocalSession?> GetCurrentSessionAsync();
}

public interface ITicketService
{
    Task<IEnumerable<LocalTypeTicket>> GetTicketTypesAsync();
    Task<string> CreateTicketAsync(LocalTicket ticket);
    Task<string> CreateTicketWithNextNumberAsync(LocalTicket ticket, int hammamPrefixeTicket);
    Task<string> GetNextTicketNumberAsync(Guid hammamId, int hammamPrefixeTicket);
    Task<(int Count, decimal Revenue)> GetTodayStatsAsync(Guid hammamId);
    Task<int> GetTotalTicketCountAsync();
    Task<int> GetTotalTicketCountAsync(Guid hammamId);
    Task<int> GetTodayCountFromServerAsync(Guid hammamId);
    Task<IEnumerable<LocalTicket>> GetUnsyncedTicketsAsync(int limit = 100);
    Task MarkAsSyncedAsync(IEnumerable<Guid> ticketIds);
}

public interface ISyncService
{
    void StartBackgroundSync();
    Task SyncNowAsync();
    Task<int> GetPendingCountAsync();
    event EventHandler<SyncEventArgs>? SyncCompleted;
}

public interface IAudioService
{
    Task PlayBeepAsync();
}

public interface IConnectivityService
{
    Task<bool> CheckConnectivityAsync();
    event EventHandler<bool>? ConnectivityChanged;
}

#endregion

#region DTOs

public record LoginResult(bool Success, string? ErrorMessage = null);

public class SyncEventArgs : EventArgs
{
    public int SyncedCount { get; set; }
    public int ErrorCount { get; set; }
    public bool Success { get; set; }
}

#endregion

#region Implementations

/// <summary>
/// Service d'authentification
/// </summary>
public class AuthService : IAuthService
{
    private readonly LocalDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SessionSettings _sessionSettings;
    private readonly IConnectivityService _connectivityService;

    public AuthService(
        LocalDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<SessionSettings> sessionSettings,
        IConnectivityService connectivityService)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _sessionSettings = sessionSettings.Value;
        _connectivityService = connectivityService;
    }

    private static string ComputePasswordHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            var normalizedUsername = username.Trim();

            if (!await _connectivityService.CheckConnectivityAsync())
            {
                return await TryOfflineLoginAsync(normalizedUsername, password);
            }

            var client = _httpClientFactory.CreateClient("HammamApi");

            var response = await client.PostAsJsonAsync("api/auth/login", new
            {
                Username = normalizedUsername,
                Password = password
            });

            if (!response.IsSuccessStatusCode)
            {
                return new LoginResult(false, "Identifiants invalides");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result == null)
            {
                return new LoginResult(false, "Réponse invalide du serveur");
            }

            // Supprimer les anciennes sessions
            var oldSessions = await _db.Sessions.ToListAsync();
            _db.Sessions.RemoveRange(oldSessions);

            // Créer la nouvelle session
            var session = new LocalSession
            {
                Id = Guid.NewGuid(),
                EmployeId = result.Employe.Id,
                Username = result.Employe.Username,
                Nom = result.Employe.Nom,
                Prenom = result.Employe.Prenom,
                HammamId = result.Employe.HammamId,
                HammamNom = result.Employe.HammamNom,
                HammamNomArabe = result.Employe.HammamNomArabe,
                HammamPrefixeTicket = result.Employe.HammamPrefixeTicket,
                Token = result.Token,
                ExpiresAt = result.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _db.Sessions.Add(session);

            // Mettre en cache le hash du mot de passe pour connexion hors ligne
            var loginUsername = (result.Employe.Username ?? normalizedUsername).Trim();
            var loweredLoginUsername = loginUsername.ToLower();

            var cachedProfile = await _db.EmployeProfiles
                .FirstOrDefaultAsync(p => p.Username.ToLower() == loweredLoginUsername)
                ?? await _db.EmployeProfiles.FirstOrDefaultAsync(p => p.Id == result.Employe.Id);

            if (cachedProfile != null)
            {
                cachedProfile.Username = loginUsername;
                cachedProfile.Prenom = result.Employe.Prenom;
                cachedProfile.Nom = result.Employe.Nom;
                cachedProfile.HammamId = result.Employe.HammamId;
                cachedProfile.HammamNom = result.Employe.HammamNom;
                cachedProfile.HammamNomArabe = result.Employe.HammamNomArabe;
                cachedProfile.HammamPrefixeTicket = result.Employe.HammamPrefixeTicket;
                cachedProfile.PasswordHash = ComputePasswordHash(password);
                cachedProfile.CachedAt = DateTime.UtcNow;
            }
            else
            {
                _db.EmployeProfiles.Add(new LocalEmployeProfile
                {
                    Id = result.Employe.Id,
                    Username = loginUsername,
                    Prenom = result.Employe.Prenom,
                    Nom = result.Employe.Nom,
                    Icone = "User1",
                    HammamId = result.Employe.HammamId,
                    HammamNom = result.Employe.HammamNom,
                    HammamNomArabe = result.Employe.HammamNomArabe,
                    HammamPrefixeTicket = result.Employe.HammamPrefixeTicket,
                    PasswordHash = ComputePasswordHash(password),
                    CachedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            return new LoginResult(true);
        }
        catch (HttpRequestException)
        {
            return await TryOfflineLoginAsync(username, password);
        }
        catch (TaskCanceledException)
        {
            return await TryOfflineLoginAsync(username, password);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la connexion");
            return new LoginResult(false, "Erreur inattendue");
        }
    }

    private async Task<LoginResult> TryOfflineLoginAsync(string username, string password)
    {
        try
        {
            var normalizedUsername = username.Trim().ToLower();
            var cachedProfile = await _db.EmployeProfiles
                .FirstOrDefaultAsync(p => p.Username.ToLower() == normalizedUsername);

            if (cachedProfile == null || string.IsNullOrEmpty(cachedProfile.PasswordHash))
            {
                return new LoginResult(false, "Impossible de joindre le serveur. Vérifiez votre connexion.");
            }

            // Vérifier le mot de passe contre le hash en cache
            var hash = ComputePasswordHash(password);
            if (hash != cachedProfile.PasswordHash)
            {
                return new LoginResult(false, "Identifiants invalides");
            }

            // Connexion hors ligne réussie — créer une session locale
            var oldSessions = await _db.Sessions.ToListAsync();
            _db.Sessions.RemoveRange(oldSessions);

            var session = new LocalSession
            {
                Id = Guid.NewGuid(),
                EmployeId = cachedProfile.Id,
                Username = cachedProfile.Username,
                Nom = cachedProfile.Nom,
                Prenom = cachedProfile.Prenom,
                HammamId = cachedProfile.HammamId,
                HammamNom = cachedProfile.HammamNom,
                HammamNomArabe = cachedProfile.HammamNomArabe,
                HammamPrefixeTicket = cachedProfile.HammamPrefixeTicket,
                Token = "offline-session",
                ExpiresAt = DateTime.UtcNow.AddHours(_sessionSettings.ExpirationHours),
                CreatedAt = DateTime.UtcNow
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            Serilog.Log.Information("Connexion hors ligne réussie pour {Username}", username);
            return new LoginResult(true);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la connexion hors ligne");
            return new LoginResult(false, "Impossible de joindre le serveur. Vérifiez votre connexion.");
        }
    }

    public async Task LogoutAsync()
    {
        var sessions = await _db.Sessions.ToListAsync();
        _db.Sessions.RemoveRange(sessions);
        await _db.SaveChangesAsync();

        // Retour à l'écran de login
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is not Views.LoginWindow)
                {
                    window.Close();
                }
            }

            var loginWindow = App.GetService<Views.LoginWindow>();
            loginWindow.Show();
        });
    }

    /// <summary>
    /// Supprime la session sans naviguer (utilisé à la fermeture de la fenêtre)
    /// </summary>
    public async Task ClearSessionAsync()
    {
        var sessions = await _db.Sessions.ToListAsync();
        _db.Sessions.RemoveRange(sessions);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasValidSessionAsync()
    {
        var session = await _db.Sessions.FirstOrDefaultAsync();

        if (session == null)
            return false;

        // Vérifier l'expiration (8 heures)
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            // Si hors ligne, prolonger la session automatiquement
            if (!await _connectivityService.CheckConnectivityAsync())
            {
                session.ExpiresAt = DateTime.UtcNow.AddHours(_sessionSettings.ExpirationHours);
                await _db.SaveChangesAsync();
                Serilog.Log.Information("Session prolongée automatiquement (hors ligne)");
                return true;
            }

            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();
            return false;
        }

        return true;
    }

    public async Task<LocalSession?> GetCurrentSessionAsync()
    {
        var session = await _db.Sessions.FirstOrDefaultAsync();

        if (session != null && session.ExpiresAt < DateTime.UtcNow)
        {
            // Si hors ligne, prolonger la session automatiquement
            if (!await _connectivityService.CheckConnectivityAsync())
            {
                session.ExpiresAt = DateTime.UtcNow.AddHours(_sessionSettings.ExpirationHours);
                await _db.SaveChangesAsync();
                Serilog.Log.Information("Session prolongée automatiquement (hors ligne)");
                return session;
            }

            // Session expirée et en ligne
            await LogoutAsync();
            return null;
        }

        return session;
    }

    private record LoginResponse(
        string Token,
        string RefreshToken,
        DateTime ExpiresAt,
        EmployeDto Employe
    );

    private record EmployeDto(
        Guid Id,
        string Username,
        string Nom,
        string Prenom,
        Guid HammamId,
        string HammamNom,
        string HammamNomArabe,
        int HammamPrefixeTicket,
        string Langue,
        string Role,
        bool Actif,
        DateTime CreatedAt,
        DateTime? LastLoginAt
    );
}

/// <summary>
/// Service de gestion des tickets
/// </summary>
public class TicketService : ITicketService
{
    private readonly LocalDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _hammamLocks = new();
    private bool _typesSynced = false;
    private const string LastTicketConfigPrefix = "LastTicketNumber:";
    private const string DeviceSuffixConfigKey = "DeviceSuffix";

    public TicketService(
        LocalDbContext db,
        IHttpClientFactory httpClientFactory,
        IConnectivityService connectivityService)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _connectivityService = connectivityService;
    }

    public async Task<IEnumerable<LocalTypeTicket>> GetTicketTypesAsync()
    {
        // Si pas encore synchronisé, essayer de charger depuis l'API
        if (!_typesSynced)
        {
            if (await _connectivityService.CheckConnectivityAsync())
            {
                await SyncTypesFromApiAsync();
            }
            else
            {
                _typesSynced = true;
            }
        }
        
        var types = await _db.TypeTickets.OrderBy(t => t.Ordre).ToListAsync();
        
        // Si toujours vide, créer des types par défaut
        if (!types.Any())
        {
            Serilog.Log.Warning("Aucun type de ticket trouvé, création des types par défaut");
            await CreateDefaultTypesAsync();
            types = await _db.TypeTickets.OrderBy(t => t.Ordre).ToListAsync();
        }
        
        return types;
    }

    private async Task SyncTypesFromApiAsync()
    {
        try
        {
            var session = await _db.Sessions.FirstOrDefaultAsync();
            if (session == null) return;

            var client = _httpClientFactory.CreateClient("HammamApi");
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await client.GetAsync($"api/typetickets/hammam/{session.HammamId}");
            
            if (response.IsSuccessStatusCode)
            {
                var apiTypes = await response.Content.ReadFromJsonAsync<List<ApiTypeTicket>>();
                
                if (apiTypes != null && apiTypes.Any())
                {
                    // Supprimer les anciens types
                    var oldTypes = await _db.TypeTickets.ToListAsync();
                    _db.TypeTickets.RemoveRange(oldTypes);
                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                    
                    // Dossier pour cache des images
                    var imageDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "HammamPOS", "images", "typetickets");
                    System.IO.Directory.CreateDirectory(imageDir);

                    // Ajouter les nouveaux types
                    foreach (var apiType in apiTypes)
                    {
                        string? localImagePath = null;

                        // Télécharger l'image si elle existe
                        if (!string.IsNullOrEmpty(apiType.ImageUrl))
                        {
                            try
                            {
                                Uri? imageUri = null;
                                if (Uri.TryCreate(apiType.ImageUrl, UriKind.Absolute, out var absoluteUri))
                                {
                                    imageUri = absoluteUri;
                                }
                                else if (client.BaseAddress != null)
                                {
                                    imageUri = new Uri(client.BaseAddress, apiType.ImageUrl);
                                }

                                if (imageUri == null)
                                    throw new InvalidOperationException($"URL image invalide: {apiType.ImageUrl}");

                                var imageResponse = await client.GetAsync(imageUri);
                                if (imageResponse.IsSuccessStatusCode)
                                {
                                    var ext = System.IO.Path.GetExtension(imageUri.AbsolutePath);
                                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                                    var fileName = $"{apiType.Id}{ext}";
                                    localImagePath = System.IO.Path.Combine(imageDir, fileName);
                                    
                                    var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
                                    await System.IO.File.WriteAllBytesAsync(localImagePath, imageBytes);
                                }
                            }
                            catch (Exception imgEx)
                            {
                                Serilog.Log.Warning(imgEx, "Impossible de télécharger l'image pour {TypeName}", apiType.Nom);
                            }
                        }

                        _db.TypeTickets.Add(new LocalTypeTicket
                        {
                            Id = apiType.Id,
                            Nom = apiType.Nom,
                            Prix = apiType.Prix,
                            Couleur = apiType.Couleur ?? "#3B82F6",
                            Icone = apiType.Icone ?? "User",
                            ImageUrl = apiType.ImageUrl,
                            LocalImagePath = localImagePath,
                            Ordre = apiType.Ordre
                        });
                    }
                    
                    await _db.SaveChangesAsync();
                    Serilog.Log.Information("Types de tickets synchronisés depuis l'API: {Count}", apiTypes.Count);
                }
            }
            _typesSynced = true;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation des types de tickets");
            _db.ChangeTracker.Clear();
            _typesSynced = true; // Marquer comme tenté pour éviter les boucles
        }
    }

    private async Task CreateDefaultTypesAsync()
    {
        var defaultTypes = new List<LocalTypeTicket>
        {
            new() { Id = Guid.NewGuid(), Nom = "HOMME", Prix = 15, Couleur = "#3B82F6", Icone = "User", Ordre = 1 },
            new() { Id = Guid.NewGuid(), Nom = "FEMME", Prix = 15, Couleur = "#EC4899", Icone = "UserCheck", Ordre = 2 },
            new() { Id = Guid.NewGuid(), Nom = "ENFANT", Prix = 10, Couleur = "#22C55E", Icone = "Baby", Ordre = 3 },
            new() { Id = Guid.NewGuid(), Nom = "DOUCHE", Prix = 5, Couleur = "#06B6D4", Icone = "Droplets", Ordre = 4 }
        };

        _db.TypeTickets.AddRange(defaultTypes);
        await _db.SaveChangesAsync();
        Serilog.Log.Information("Types de tickets par défaut créés");
    }

    private SemaphoreSlim GetHammamLock(Guid hammamId)
    {
        return _hammamLocks.GetOrAdd(hammamId, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<string> CreateTicketWithNextNumberAsync(LocalTicket ticket, int hammamPrefixeTicket)
    {
        var hammamLock = GetHammamLock(ticket.HammamId);
        await hammamLock.WaitAsync();

        try
        {
            const int maxAttempts = 5;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using var transaction = await _db.Database.BeginTransactionAsync();

                    var ticketNumber = await ReserveNextTicketNumberInternalAsync(ticket.HammamId, hammamPrefixeTicket);
                    ticket.TicketNumber = ticketNumber;

                    _db.Tickets.Add(ticket);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return ticketNumber;
                }
                catch (DbUpdateException ex) when (IsTicketNumberUniqueCollision(ex) && attempt < maxAttempts)
                {
                    _db.Entry(ticket).State = EntityState.Detached;
                    Serilog.Log.Warning(ex,
                        "Collision numéro ticket détectée (tentative {Attempt}/{Max}), retry automatique",
                        attempt, maxAttempts);
                }
            }

            throw new InvalidOperationException("Impossible de générer un numéro de ticket unique après plusieurs tentatives.");
        }
        catch
        {
            throw;
        }
        finally
        {
            hammamLock.Release();
        }
    }

    public async Task<string> CreateTicketAsync(LocalTicket ticket)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync();
        var hammamPrefixeTicket = session?.HammamPrefixeTicket ?? 100000;
        return await CreateTicketWithNextNumberAsync(ticket, hammamPrefixeTicket);
    }

    public async Task<string> GetNextTicketNumberAsync(Guid hammamId, int hammamPrefixeTicket)
    {
        var lastTicketNumber = await GetLastTicketNumberSnapshotAsync(hammamId, hammamPrefixeTicket);
        var nextNumericNumber = lastTicketNumber + 1;
        var deviceSuffix = await GetOrCreateDeviceSuffixAsync();
        return $"{nextNumericNumber}-{deviceSuffix}";
    }

    public async Task<(int Count, decimal Revenue)> GetTodayStatsAsync(Guid hammamId)
    {
        // Fuseau horaire du Maroc (UTC+1)
        // Calculer les bornes du jour local en UTC
        var moroccoOffset = TimeSpan.FromHours(1);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = nowUtc + moroccoOffset;
        var todayLocalStart = nowLocal.Date;
        // Convertir les bornes locales en UTC
        var todayUtcStart = todayLocalStart - moroccoOffset;
        var tomorrowUtcStart = todayUtcStart.AddDays(1);

        var tickets = await _db.Tickets
            .Where(t => t.HammamId == hammamId && 
                        t.CreatedAt >= todayUtcStart && 
                        t.CreatedAt < tomorrowUtcStart)
            .ToListAsync();

        // Si aucun ticket local trouvé, essayer de récupérer le compteur du serveur
        if (tickets.Count == 0)
        {
            try
            {
                var serverCount = await GetTodayCountFromServerAsync(hammamId);
                if (serverCount > 0)
                {
                    Serilog.Log.Information(
                        "Compteur local vide, utilisation du compteur serveur: {Count}", serverCount);
                    return (serverCount, 0);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Impossible de récupérer le compteur serveur, utilisation du local");
            }
        }

        return (tickets.Count, tickets.Sum(t => t.Prix));
    }

    public async Task<int> GetTotalTicketCountAsync()
    {
        var session = await _db.Sessions.FirstOrDefaultAsync();
        if (session == null)
        {
            return await _db.Tickets.CountAsync();
        }

        var localCount = await _db.Tickets.CountAsync(t => t.HammamId == session.HammamId);
        var serverCount = await GetTotalCountFromServerAsync(session.HammamId);
        return Math.Max(localCount, serverCount);
    }

    public async Task<int> GetTotalTicketCountAsync(Guid hammamId)
    {
        var localCount = await _db.Tickets.CountAsync(t => t.HammamId == hammamId);
        var serverCount = await GetTotalCountFromServerAsync(hammamId);
        return Math.Max(localCount, serverCount);
    }

    public async Task<int> GetTodayCountFromServerAsync(Guid hammamId)
    {
        try
        {
            if (!await _connectivityService.CheckConnectivityAsync())
            {
                return 0;
            }

            var session = await _db.Sessions.FirstOrDefaultAsync();
            if (session == null) return 0;

            var client = _httpClientFactory.CreateClient("HammamApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await client.GetAsync($"api/tickets/count/today?hammamId={hammamId}");
            if (response.IsSuccessStatusCode)
            {
                var count = await response.Content.ReadFromJsonAsync<int>();
                return count;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Erreur lors de la récupération du compteur serveur");
        }
        return 0;
    }

    private async Task<int> GetTotalCountFromServerAsync(Guid hammamId)
    {
        try
        {
            if (!await _connectivityService.CheckConnectivityAsync())
            {
                return 0;
            }

            var session = await _db.Sessions.FirstOrDefaultAsync();
            if (session == null) return 0;

            var client = _httpClientFactory.CreateClient("HammamApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await client.GetAsync($"api/tickets/count/total?hammamId={hammamId}");
            if (response.IsSuccessStatusCode)
            {
                var count = await response.Content.ReadFromJsonAsync<int>();
                return count;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Erreur lors de la récupération du compteur total serveur");
        }

        return 0;
    }

    private async Task<int> GetLastTicketNumberSnapshotAsync(Guid hammamId, int hammamPrefixeTicket)
    {
        var configKey = $"{LastTicketConfigPrefix}{hammamId}";
        var config = await _db.Configs.FirstOrDefaultAsync(c => c.Key == configKey);

        var lastTicketFromConfig = 0;
        if (config != null)
        {
            _ = int.TryParse(config.Value, out lastTicketFromConfig);
        }

        var localCount = await _db.Tickets.CountAsync(t => t.HammamId == hammamId);
        var baseTicketNumber = hammamPrefixeTicket + localCount;

        if (config == null && await _connectivityService.CheckConnectivityAsync())
        {
            var serverCount = await GetTotalCountFromServerAsync(hammamId);
            baseTicketNumber = hammamPrefixeTicket + Math.Max(localCount, serverCount);
        }

        return Math.Max(lastTicketFromConfig, baseTicketNumber);
    }

    private async Task<string> ReserveNextTicketNumberInternalAsync(Guid hammamId, int hammamPrefixeTicket)
    {
        var configKey = $"{LastTicketConfigPrefix}{hammamId}";
        var currentLastTicketNumber = await GetLastTicketNumberSnapshotAsync(hammamId, hammamPrefixeTicket);
        var nextTicketNumber = currentLastTicketNumber + 1;
        var deviceSuffix = await GetOrCreateDeviceSuffixAsync();

        var config = await _db.Configs.FirstOrDefaultAsync(c => c.Key == configKey);
        if (config == null)
        {
            config = new LocalConfig
            {
                Key = configKey,
                Value = nextTicketNumber.ToString(),
                UpdatedAt = DateTime.UtcNow
            };
            _db.Configs.Add(config);
        }
        else
        {
            config.Value = nextTicketNumber.ToString();
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return $"{nextTicketNumber}-{deviceSuffix}";
    }

    private async Task<string> GetOrCreateDeviceSuffixAsync()
    {
        var existing = await _db.Configs.FirstOrDefaultAsync(c => c.Key == DeviceSuffixConfigKey);
        if (existing != null && IsValidDeviceSuffix(existing.Value))
        {
            return existing.Value;
        }

        var suffix = CreateRandomDeviceSuffix();

        if (existing == null)
        {
            _db.Configs.Add(new LocalConfig
            {
                Key = DeviceSuffixConfigKey,
                Value = suffix,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = suffix;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return suffix;
    }

    private static bool IsTicketNumberUniqueCollision(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_tickets_hammam_ticket_number", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE constraint failed: tickets.HammamId, tickets.TicketNumber", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidDeviceSuffix(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length >= 4
            && value.All(char.IsLetterOrDigit);
    }

    private static string CreateRandomDeviceSuffix()
    {
        Span<byte> bytes = stackalloc byte[3];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    public async Task<IEnumerable<LocalTicket>> GetUnsyncedTicketsAsync(int limit = 100)
    {
        return await _db.Tickets
            .Where(t => t.SyncStatus == "Pending")
            .OrderBy(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task MarkAsSyncedAsync(IEnumerable<Guid> ticketIds)
    {
        var tickets = await _db.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            ticket.SyncStatus = "Synced";
            ticket.SyncedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    // DTO pour recevoir les types de l'API
    private record ApiTypeTicket(
        Guid Id,
        string Nom,
        decimal Prix,
        string? Couleur,
        string? Icone,
        string? ImageUrl,
        int Ordre,
        bool Actif,
        Guid? HammamId
    );
}

/// <summary>
/// Service de synchronisation avec le serveur
/// </summary>
public class SyncService : ISyncService
{
    private readonly ITicketService _ticketService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly LocalDbContext _db;
    private readonly SyncSettings _settings;
    private Timer? _syncTimer;

    public event EventHandler<SyncEventArgs>? SyncCompleted;

    public SyncService(
        ITicketService ticketService,
        IHttpClientFactory httpClientFactory,
        IConnectivityService connectivityService,
        LocalDbContext db,
        IOptions<SyncSettings> settings)
    {
        _ticketService = ticketService;
        _httpClientFactory = httpClientFactory;
        _connectivityService = connectivityService;
        _db = db;
        _settings = settings.Value;
    }

    public void StartBackgroundSync()
    {
        var interval = TimeSpan.FromMinutes(_settings.IntervalMinutes);
        _syncTimer = new Timer(async _ => await TrySyncAsync(), null, interval, interval);
        
        Serilog.Log.Information("Synchronisation automatique démarrée (toutes les {Minutes} minutes)", 
            _settings.IntervalMinutes);
    }

    private async Task TrySyncAsync()
    {
        try
        {
            if (!await _connectivityService.CheckConnectivityAsync())
            {
                Serilog.Log.Debug("Pas de connexion internet, synchronisation reportée");
                return;
            }

            await SyncNowAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation automatique");
        }
    }

    public async Task SyncNowAsync()
    {
        var session = await _db.Sessions.FirstOrDefaultAsync();
        if (session == null) return;

        var unsyncedTickets = await _ticketService.GetUnsyncedTicketsAsync(_settings.BatchSize);
        var ticketList = unsyncedTickets.ToList();

        if (!ticketList.Any())
        {
            Serilog.Log.Debug("Aucun ticket à synchroniser");
            return;
        }

        Serilog.Log.Information("Synchronisation de {Count} tickets...", ticketList.Count);

        try
        {
            var client = _httpClientFactory.CreateClient("HammamApi");
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

            var response = await client.PostAsJsonAsync("api/tickets/sync", new
            {
                Tickets = ticketList.Select(t => new
                {
                    t.Id,
                    TicketNumber = string.IsNullOrWhiteSpace(t.TicketNumber)
                        ? $"LEGACY-{t.Id.ToString("N")[..8]}"
                        : t.TicketNumber,
                    t.TypeTicketId,
                    t.EmployeId,
                    t.HammamId,
                    t.Prix,
                    t.CreatedAt,
                    t.DeviceId
                })
            });

            if (response.IsSuccessStatusCode)
            {
                // Lire la réponse du serveur pour savoir quels tickets ont échoué
                var syncResult = await response.Content.ReadFromJsonAsync<SyncResponse>();
                
                var failedIds = syncResult?.FailedTicketIds?.ToHashSet() ?? new HashSet<Guid>();
                var successIds = ticketList
                    .Where(t => !failedIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToList();

                if (successIds.Any())
                {
                    await _ticketService.MarkAsSyncedAsync(successIds);
                }
                
                var syncedCount = successIds.Count;
                var errorCount = failedIds.Count;

                Serilog.Log.Information(
                    "Synchronisation terminée: {Synced} réussis, {Errors} échoués sur {Total} total",
                    syncedCount, errorCount, ticketList.Count);
                
                SyncCompleted?.Invoke(this, new SyncEventArgs
                {
                    SyncedCount = syncedCount,
                    ErrorCount = errorCount,
                    Success = errorCount == 0
                });
            }
            else
            {
                Serilog.Log.Warning("Échec de la synchronisation: {StatusCode}", response.StatusCode);
                
                SyncCompleted?.Invoke(this, new SyncEventArgs
                {
                    SyncedCount = 0,
                    ErrorCount = ticketList.Count,
                    Success = false
                });
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation");
            
            SyncCompleted?.Invoke(this, new SyncEventArgs
            {
                SyncedCount = 0,
                ErrorCount = ticketList.Count,
                Success = false
            });
        }
    }

    // DTO pour lire la réponse du serveur
    private record SyncResponse(
        int TotalReceived,
        int Inserted,
        int Updated,
        int Errors,
        IEnumerable<Guid>? FailedTicketIds
    );

    public async Task<int> GetPendingCountAsync()
    {
        return await _db.Tickets.CountAsync(t => t.SyncStatus == "Pending");
    }
}

/// <summary>
/// Service audio pour les confirmations
/// </summary>
public class AudioService : IAudioService
{
    public Task PlayBeepAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                // Son système simple
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Impossible de jouer le son");
            }
        });
    }
}

/// <summary>
/// Service de vérification de la connectivité
/// </summary>
public class ConnectivityService : IConnectivityService
{
    private bool _lastState = true;
    private readonly IHttpClientFactory _httpClientFactory;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        
        // Vérifier périodiquement
        var timer = new Timer(async _ => await CheckAndNotifyAsync(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
                var client = _httpClientFactory.CreateClient("HammamApiFast");
            
            var response = await client.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task CheckAndNotifyAsync()
    {
        var currentState = await CheckConnectivityAsync();
        
        if (currentState != _lastState)
        {
            _lastState = currentState;
            ConnectivityChanged?.Invoke(this, currentState);
            
            Serilog.Log.Information("Changement de connectivité: {State}", 
                currentState ? "En ligne" : "Hors ligne");
        }
    }
}

#endregion
