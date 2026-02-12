using HammamDesktop.Data;
using HammamDesktop.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;

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
    Task CreateTicketAsync(LocalTicket ticket);
    Task<(int Count, decimal Revenue)> GetTodayStatsAsync(Guid hammamId);
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

    public AuthService(
        LocalDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<SessionSettings> sessionSettings)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _sessionSettings = sessionSettings.Value;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("HammamApi");
            
            var response = await client.PostAsJsonAsync("api/auth/login", new
            {
                Username = username,
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
            await _db.SaveChangesAsync();

            return new LoginResult(true);
        }
        catch (HttpRequestException)
        {
            return new LoginResult(false, "Impossible de joindre le serveur. Vérifiez votre connexion.");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la connexion");
            return new LoginResult(false, "Erreur inattendue");
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

    public async Task<bool> HasValidSessionAsync()
    {
        var session = await _db.Sessions.FirstOrDefaultAsync();
        
        if (session == null)
            return false;

        // Vérifier l'expiration (8 heures)
        if (session.ExpiresAt < DateTime.UtcNow)
        {
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
            // Session expirée
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
    private bool _typesSynced = false;

    public TicketService(LocalDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<LocalTypeTicket>> GetTicketTypesAsync()
    {
        // Si pas encore synchronisé, essayer de charger depuis l'API
        if (!_typesSynced)
        {
            await SyncTypesFromApiAsync();
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
                                var imageResponse = await client.GetAsync(apiType.ImageUrl);
                                if (imageResponse.IsSuccessStatusCode)
                                {
                                    var ext = System.IO.Path.GetExtension(new Uri(apiType.ImageUrl).AbsolutePath);
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

    public async Task CreateTicketAsync(LocalTicket ticket)
    {
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();
    }

    public async Task<(int Count, decimal Revenue)> GetTodayStatsAsync(Guid hammamId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var tickets = await _db.Tickets
            .Where(t => t.HammamId == hammamId && 
                        t.CreatedAt >= today && 
                        t.CreatedAt < tomorrow)
            .ToListAsync();

        return (tickets.Count, tickets.Sum(t => t.Prix));
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
                await _ticketService.MarkAsSyncedAsync(ticketList.Select(t => t.Id));
                
                Serilog.Log.Information("Synchronisation réussie: {Count} tickets", ticketList.Count);
                
                SyncCompleted?.Invoke(this, new SyncEventArgs
                {
                    SyncedCount = ticketList.Count,
                    ErrorCount = 0,
                    Success = true
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
            var client = _httpClientFactory.CreateClient("HammamApi");
            client.Timeout = TimeSpan.FromSeconds(5);
            
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
