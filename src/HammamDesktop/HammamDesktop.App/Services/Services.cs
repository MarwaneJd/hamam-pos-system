using HammamDesktop.Data;
using HammamDesktop.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace HammamDesktop.Services;

#region Interfaces

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task LogoutAsync();

    /// <summary>Supprime la session sans naviguer (fermeture de la fenêtre).</summary>
    Task ClearSessionAsync();

    Task<bool> HasValidSessionAsync();
    Task<LocalSession?> GetCurrentSessionAsync();

    /// <summary>
    /// Renvoie un token JWT utilisable. Si le token est "offline" ou expiré et que la
    /// connexion est disponible, ré-authentifie silencieusement avec le mot de passe
    /// chiffré DPAPI. Renvoie null si aucun token valide ne peut être obtenu.
    /// </summary>
    Task<string?> EnsureValidTokenAsync(bool forceRefresh = false);

    /// <summary>
    /// Télécharge les accès hors ligne de tous les employés du hammam
    /// (pour que chaque employé puisse se connecter hors ligne sur n'importe quel poste).
    /// </summary>
    Task RefreshOfflineCredentialsAsync(bool force = false);

    /// <summary>
    /// Token pour la synchronisation en arrière-plan : celui de la session ouverte, ou, si personne
    /// n'est connecté (écran de connexion, appli fermée puis relancée), un token obtenu avec les accès
    /// chiffrés d'un employé déjà connu sur ce poste. Les tickets partent ainsi même sans session.
    /// </summary>
    Task<string?> GetSyncTokenAsync(bool forceRefresh = false);
}

public interface ITicketService
{
    Task<IEnumerable<LocalTypeTicket>> GetTicketTypesAsync();
    Task<string> CreateTicketWithNextNumberAsync(LocalTicket ticket, int hammamPrefixeTicket);
    Task<string> GetNextTicketNumberAsync(Guid hammamId, int hammamPrefixeTicket);
    Task<(int Count, decimal Revenue)> GetTodayStatsAsync(Guid hammamId);
    Task<List<LocalTicket>> GetUnsyncedTicketsAsync(int limit = 100);
    Task MarkAsSyncedAsync(IReadOnlyCollection<Guid> ticketIds);
    Task MarkSyncFailedAsync(IReadOnlyCollection<Guid> ticketIds, string reason);
}

public interface ISyncService
{
    void StartBackgroundSync();

    /// <summary>Synchronise TOUS les tickets en attente (par lots successifs).</summary>
    Task SyncNowAsync();

    /// <summary>Déclenche une synchro en arrière-plan si la connexion est disponible (non bloquant).</summary>
    void RequestSync();

    Task<int> GetPendingCountAsync();
    event EventHandler<SyncEventArgs>? SyncCompleted;
}

public interface IAudioService
{
    Task PlayBeepAsync();
}

public interface IConnectivityService
{
    /// <summary>Vérifie la connexion (appel réseau, 2 s max) et met à jour <see cref="IsOnline"/>.</summary>
    Task<bool> CheckConnectivityAsync();

    /// <summary>Dernier état connu, sans appel réseau (pour les chemins critiques comme la vente).</summary>
    bool IsOnline { get; }

    event EventHandler<bool>? ConnectivityChanged;
}

#endregion

#region DTOs

public record LoginResult(bool Success, string? ErrorMessage = null);

public class SyncEventArgs : EventArgs
{
    public int SyncedCount { get; set; }
    public int ErrorCount { get; set; }
    public int PendingCount { get; set; }
    public bool Success { get; set; }
}

internal record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    EmployeDto Employe
);

internal record EmployeDto(
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

internal record OfflineCredentialDto(
    Guid Id,
    string Username,
    string Prenom,
    string Nom,
    string? Icone,
    Guid HammamId,
    string HammamNom,
    string HammamNomArabe,
    int HammamPrefixeTicket,
    string PasswordHash
);

internal static class ApiClient
{
    public static HttpClient CreateAuthorized(IHttpClientFactory factory, string token)
    {
        var client = factory.CreateClient("HammamApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

#endregion

#region Implementations

/// <summary>
/// Service d'authentification : connexion en ligne / hors ligne transparente,
/// ré-authentification silencieuse au retour de la connexion.
/// </summary>
public class AuthService : IAuthService
{
    public const string OfflineToken = "offline-session";

    private static readonly TimeSpan CredentialsRefreshInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan TokenSafetyMargin = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<LocalDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SessionSettings _sessionSettings;
    private readonly IConnectivityService _connectivityService;
    private readonly SemaphoreSlim _reauthLock = new(1, 1);
    private readonly SemaphoreSlim _credentialsLock = new(1, 1);
    private DateTime _lastCredentialsRefreshUtc = DateTime.MinValue;
    private string? _backgroundToken;
    private DateTime _backgroundTokenExpiresUtc = DateTime.MinValue;

    public AuthService(
        IDbContextFactory<LocalDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<SessionSettings> sessionSettings,
        IConnectivityService connectivityService)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _sessionSettings = sessionSettings.Value;
        _connectivityService = connectivityService;
    }

    private static string ComputePasswordHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static bool IsRealToken(string? token) =>
        !string.IsNullOrEmpty(token) && token != OfflineToken;

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        var normalizedUsername = username.Trim();

        try
        {
            if (!await _connectivityService.CheckConnectivityAsync())
            {
                return await TryOfflineLoginAsync(normalizedUsername, password);
            }

            var (login, invalidCredentials) = await CallLoginApiAsync(normalizedUsername, password);
            if (invalidCredentials)
            {
                return new LoginResult(false, "Identifiants invalides");
            }

            if (login == null)
            {
                // Serveur joignable mais réponse inexploitable → on tente le hors ligne
                return await TryOfflineLoginAsync(normalizedUsername, password);
            }

            await SaveOnlineSessionAsync(login, password);

            // Récupérer les accès hors ligne de tous les employés du hammam (non bloquant)
            _ = RefreshOfflineCredentialsAsync(force: true);

            return new LoginResult(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Serilog.Log.Warning(ex, "Serveur injoignable pendant la connexion, bascule hors ligne");
            return await TryOfflineLoginAsync(normalizedUsername, password);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la connexion");
            return new LoginResult(false, "Erreur inattendue");
        }
    }

    /// <summary>
    /// Appelle api/auth/login. Renvoie (null, true) si les identifiants sont refusés,
    /// (null, false) si la réponse est inexploitable. Lève HttpRequestException /
    /// TaskCanceledException si le serveur est injoignable.
    /// </summary>
    private async Task<(LoginResponse? Login, bool InvalidCredentials)> CallLoginApiAsync(string username, string password)
    {
        var client = _httpClientFactory.CreateClient("HammamApi");
        var response = await client.PostAsJsonAsync("api/auth/login", new { Username = username, Password = password });

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return (null, true);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Login API a répondu {(int)response.StatusCode}");
        }

        return (await response.Content.ReadFromJsonAsync<LoginResponse>(), false);
    }

    private async Task SaveOnlineSessionAsync(LoginResponse login, string password)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        db.Sessions.RemoveRange(await db.Sessions.ToListAsync());

        var emp = login.Employe;
        db.Sessions.Add(new LocalSession
        {
            Id = Guid.NewGuid(),
            EmployeId = emp.Id,
            Username = emp.Username,
            Nom = emp.Nom,
            Prenom = emp.Prenom,
            HammamId = emp.HammamId,
            HammamNom = emp.HammamNom,
            HammamNomArabe = emp.HammamNomArabe,
            HammamPrefixeTicket = emp.HammamPrefixeTicket,
            Token = login.Token,
            ExpiresAt = login.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        // Le profil est identifié par l'ID employé (et non par le username :
        // le même username existe dans plusieurs hammams avec des mots de passe différents).
        var profile = await db.EmployeProfiles.FirstOrDefaultAsync(p => p.Id == emp.Id);
        if (profile == null)
        {
            profile = new LocalEmployeProfile { Id = emp.Id, Icone = "User1" };
            db.EmployeProfiles.Add(profile);
        }

        profile.Username = emp.Username;
        profile.Prenom = emp.Prenom;
        profile.Nom = emp.Nom;
        profile.HammamId = emp.HammamId;
        profile.HammamNom = emp.HammamNom;
        profile.HammamNomArabe = emp.HammamNomArabe;
        profile.HammamPrefixeTicket = emp.HammamPrefixeTicket;
        profile.PasswordHash = ComputePasswordHash(password);
        profile.EncryptedPassword = CryptoHelper.Protect(password);
        profile.CachedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private async Task<LoginResult> TryOfflineLoginAsync(string username, string password)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var normalizedUsername = username.Trim().ToLower();
            var candidates = await db.EmployeProfiles
                .Where(p => p.Username.ToLower() == normalizedUsername)
                .ToListAsync();

            if (candidates.Count == 0)
            {
                return new LoginResult(false,
                    "Impossible de joindre le serveur et ce profil n'est pas encore activé hors ligne sur ce poste.");
            }

            var localHash = ComputePasswordHash(password);

            // Le hash serveur (BCrypt) fait foi quand il est connu : c'est le mot de passe actuel de CET employé.
            // Le hash local (SHA) ne sert que pour les profils jamais rafraîchis depuis le serveur
            // (d'anciennes versions pouvaient y mélanger deux employés de même username).
            var match = await Task.Run(() => candidates.FirstOrDefault(p =>
                !string.IsNullOrEmpty(p.ServerPasswordHash)
                    ? VerifyServerHash(password, CryptoHelper.Unprotect(p.ServerPasswordHash))
                    : !string.IsNullOrEmpty(p.PasswordHash) && p.PasswordHash == localHash));

            if (match == null)
            {
                return candidates.Any(p => !string.IsNullOrEmpty(p.PasswordHash) || !string.IsNullOrEmpty(p.ServerPasswordHash))
                    ? new LoginResult(false, "Identifiants invalides")
                    : new LoginResult(false, "Impossible de joindre le serveur. Vérifiez votre connexion.");
            }

            // Mémoriser pour la ré-auth silencieuse au retour de la connexion
            match.PasswordHash = localHash;
            match.EncryptedPassword = CryptoHelper.Protect(password);

            db.Sessions.RemoveRange(await db.Sessions.ToListAsync());
            db.Sessions.Add(new LocalSession
            {
                Id = Guid.NewGuid(),
                EmployeId = match.Id,
                Username = match.Username,
                Nom = match.Nom,
                Prenom = match.Prenom,
                HammamId = match.HammamId,
                HammamNom = match.HammamNom,
                HammamNomArabe = match.HammamNomArabe,
                HammamPrefixeTicket = match.HammamPrefixeTicket,
                Token = OfflineToken,
                ExpiresAt = DateTime.UtcNow.AddHours(_sessionSettings.ExpirationHours),
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            Serilog.Log.Information("Connexion hors ligne réussie pour {Username}", username);
            return new LoginResult(true);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la connexion hors ligne");
            return new LoginResult(false, "Impossible de joindre le serveur. Vérifiez votre connexion.");
        }
    }

    private static bool VerifyServerHash(string password, string? bcryptHash)
    {
        if (string.IsNullOrEmpty(bcryptHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, bcryptHash);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Hash serveur invalide en cache");
            return false;
        }
    }

    public async Task<string?> EnsureValidTokenAsync(bool forceRefresh = false)
    {
        var session = await ReadSessionAsync();
        if (session == null)
            return null;

        if (!forceRefresh && IsTokenUsable(session))
            return session.Token;

        if (!await _connectivityService.CheckConnectivityAsync())
            return null;

        await _reauthLock.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            session = await db.Sessions.FirstOrDefaultAsync();
            if (session == null)
                return null;

            // Un autre appel a peut-être déjà rafraîchi le token pendant l'attente
            if (!forceRefresh && IsTokenUsable(session))
                return session.Token;

            var profile = await db.EmployeProfiles.FirstOrDefaultAsync(p => p.Id == session.EmployeId);
            var password = CryptoHelper.Unprotect(profile?.EncryptedPassword);
            if (password == null)
            {
                Serilog.Log.Warning("Aucun mot de passe chiffré pour {Username} : ré-authentification impossible", session.Username);
                return IsRealToken(session.Token) ? session.Token : null;
            }

            var (login, invalidCredentials) = await CallLoginApiAsync(session.Username, password);
            if (invalidCredentials || login == null)
            {
                Serilog.Log.Warning("Ré-authentification refusée pour {Username} (mot de passe changé sur le serveur ?)", session.Username);
                return null;
            }

            if (login.Employe.Id != session.EmployeId)
            {
                Serilog.Log.Warning(
                    "Ré-authentification : le serveur a renvoyé l'employé {Returned} au lieu de {Expected}, token ignoré",
                    login.Employe.Id, session.EmployeId);
                return null;
            }

            session.Token = login.Token;
            session.ExpiresAt = login.ExpiresAt;
            await db.SaveChangesAsync();

            Serilog.Log.Information("Token rafraîchi silencieusement pour {Username}", session.Username);
            return session.Token;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Serilog.Log.Debug(ex, "Ré-authentification impossible (serveur injoignable)");
            return null;
        }
        finally
        {
            _reauthLock.Release();
        }
    }

    public async Task<string?> GetSyncTokenAsync(bool forceRefresh = false)
    {
        if (await ReadSessionAsync() != null)
        {
            var sessionToken = await EnsureValidTokenAsync(forceRefresh);
            if (sessionToken != null)
                return sessionToken;
            // Session ouverte mais non ré-authentifiable (ex. connexion hors ligne avec un mot de passe
            // changé depuis) : on synchronise quand même avec les accès d'un autre employé du poste.
        }

        if (!forceRefresh && _backgroundToken != null && _backgroundTokenExpiresUtc > DateTime.UtcNow + TokenSafetyMargin)
            return _backgroundToken;

        if (!await _connectivityService.CheckConnectivityAsync())
            return null;

        await _reauthLock.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var candidates = await db.EmployeProfiles.AsNoTracking()
                .Where(p => p.EncryptedPassword != null && p.EncryptedPassword != "")
                .OrderByDescending(p => p.CachedAt)
                .Take(3)
                .ToListAsync();

            foreach (var profile in candidates)
            {
                var password = CryptoHelper.Unprotect(profile.EncryptedPassword);
                if (password == null) continue;

                var (login, _) = await CallLoginApiAsync(profile.Username, password);
                if (login == null || login.Employe.Id != profile.Id) continue;

                _backgroundToken = login.Token;
                _backgroundTokenExpiresUtc = login.ExpiresAt;
                Serilog.Log.Information("Token de synchronisation obtenu sans session ouverte ({Username})", profile.Username);
                return _backgroundToken;
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Serilog.Log.Debug(ex, "Token de synchronisation indisponible (serveur injoignable)");
            return null;
        }
        finally
        {
            _reauthLock.Release();
        }
    }

    private static bool IsTokenUsable(LocalSession session) =>
        IsRealToken(session.Token) && session.ExpiresAt > DateTime.UtcNow + TokenSafetyMargin;

    public async Task RefreshOfflineCredentialsAsync(bool force = false)
    {
        if (!force && DateTime.UtcNow - _lastCredentialsRefreshUtc < CredentialsRefreshInterval)
            return;

        // Une seule mise à jour à la fois (login + synchro peuvent la déclencher en même temps)
        await _credentialsLock.WaitAsync();
        try
        {
            if (!force && DateTime.UtcNow - _lastCredentialsRefreshUtc < CredentialsRefreshInterval)
                return;

            var token = await EnsureValidTokenAsync();
            if (token == null)
                return;

            var session = await ReadSessionAsync();
            if (session == null)
                return;

            var client = ApiClient.CreateAuthorized(_httpClientFactory, token);
            var response = await client.GetAsync("api/auth/offline-credentials");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Serilog.Log.Debug("Le serveur ne fournit pas encore les accès hors ligne (endpoint absent)");
                _lastCredentialsRefreshUtc = DateTime.UtcNow;
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                Serilog.Log.Warning("Récupération des accès hors ligne échouée: {Status}", response.StatusCode);
                return;
            }

            var credentials = await response.Content.ReadFromJsonAsync<List<OfflineCredentialDto>>() ?? new();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var existing = await db.EmployeProfiles.ToListAsync();
            var receivedIds = credentials.Select(c => c.Id).ToHashSet();

            foreach (var cred in credentials)
            {
                var profile = existing.FirstOrDefault(p => p.Id == cred.Id);
                if (profile == null)
                {
                    profile = new LocalEmployeProfile { Id = cred.Id };
                    db.EmployeProfiles.Add(profile);
                }
                else if (CryptoHelper.Unprotect(profile.ServerPasswordHash) != cred.PasswordHash)
                {
                    // Hash serveur nouveau ou changé : on ne garde les secrets locaux que s'ils correspondent
                    // au mot de passe ACTUEL de cet employé. Couvre le changement de mot de passe (sans casser
                    // la ré-auth de l'employé qui vient de se connecter avec le nouveau) et les anciennes
                    // lignes où un autre employé de même username avait laissé son mot de passe.
                    var localPassword = CryptoHelper.Unprotect(profile.EncryptedPassword);
                    var stillValid = localPassword != null
                        && await Task.Run(() => VerifyServerHash(localPassword, cred.PasswordHash));
                    if (!stillValid)
                    {
                        profile.PasswordHash = "";
                        profile.EncryptedPassword = null;
                    }
                }

                profile.Username = cred.Username;
                profile.Prenom = cred.Prenom;
                profile.Nom = cred.Nom;
                profile.Icone = string.IsNullOrEmpty(cred.Icone) ? "User1" : cred.Icone;
                profile.HammamId = cred.HammamId;
                profile.HammamNom = cred.HammamNom;
                profile.HammamNomArabe = cred.HammamNomArabe;
                profile.HammamPrefixeTicket = cred.HammamPrefixeTicket;
                // Chiffré DPAPI (compte Windows) : une copie de la base sortie du PC est inexploitable
                profile.ServerPasswordHash = CryptoHelper.Protect(cred.PasswordHash);
                profile.CachedAt = DateTime.UtcNow;
            }

            // Employés désactivés/supprimés de CE hammam : retirer l'accès hors ligne
            // (+ lignes invalides laissées par d'anciennes versions : Id vide)
            var revoked = existing
                .Where(p => p.Id == Guid.Empty
                    || (p.HammamId == session.HammamId && !receivedIds.Contains(p.Id) && p.Id != session.EmployeId))
                .ToList();
            db.EmployeProfiles.RemoveRange(revoked);

            await db.SaveChangesAsync();
            _lastCredentialsRefreshUtc = DateTime.UtcNow;

            Serilog.Log.Information(
                "Accès hors ligne mis à jour: {Count} employés, {Revoked} révoqués",
                credentials.Count, revoked.Count);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Impossible de mettre à jour les accès hors ligne");
        }
        finally
        {
            _credentialsLock.Release();
        }
    }

    private async Task<LocalSession?> ReadSessionAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Sessions.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task LogoutAsync()
    {
        await ClearSessionAsync();

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

    public async Task ClearSessionAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Sessions.RemoveRange(await db.Sessions.ToListAsync());
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Au démarrage : une session restante (fermeture brutale) n'est reprise que si elle
    /// n'est pas expirée, ou si on est hors ligne (pour ne jamais bloquer la caisse).
    /// </summary>
    public async Task<bool> HasValidSessionAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var session = await db.Sessions.FirstOrDefaultAsync();

        if (session == null)
            return false;

        if (session.ExpiresAt >= DateTime.UtcNow)
            return true;

        if (!await _connectivityService.CheckConnectivityAsync())
        {
            session.ExpiresAt = DateTime.UtcNow.AddHours(_sessionSettings.ExpirationHours);
            await db.SaveChangesAsync();
            Serilog.Log.Information("Session prolongée automatiquement (hors ligne)");
            return true;
        }

        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
        return false;
    }

    /// <summary>
    /// Pendant l'utilisation, la session n'est jamais coupée sur expiration :
    /// le token est rafraîchi silencieusement par <see cref="EnsureValidTokenAsync"/>.
    /// </summary>
    public Task<LocalSession?> GetCurrentSessionAsync() => ReadSessionAsync();
}

/// <summary>
/// Service de gestion des tickets (local-first : aucune vente ne dépend du réseau)
/// </summary>
public class TicketService : ITicketService
{
    private const string LastTicketConfigPrefix = "LastTicketNumber:";
    private const string DeviceSuffixConfigKey = "DeviceSuffix";
    private const string StatusPending = "Pending";
    private const string StatusSynced = "Synced";

    private static readonly TimeSpan MoroccoOffset = TimeSpan.FromHours(1);

    private readonly IDbContextFactory<LocalDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly IAuthService _authService;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _hammamLocks = new();
    private volatile bool _typesSynced;

    public TicketService(
        IDbContextFactory<LocalDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        IConnectivityService connectivityService,
        IAuthService authService)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _connectivityService = connectivityService;
        _authService = authService;
    }

    public async Task<IEnumerable<LocalTypeTicket>> GetTicketTypesAsync()
    {
        if (!_typesSynced)
        {
            if (await _connectivityService.CheckConnectivityAsync())
            {
                await SyncTypesFromApiAsync();
            }
            _typesSynced = true;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var types = await db.TypeTickets.AsNoTracking().OrderBy(t => t.Ordre).ToListAsync();

        if (types.Count == 0)
        {
            Serilog.Log.Warning("Aucun type de ticket trouvé, création des types par défaut");
            types = CreateDefaultTypes();
            db.TypeTickets.AddRange(types);
            await db.SaveChangesAsync();
            Serilog.Log.Information("Types de tickets par défaut créés");
        }

        return types;
    }

    private async Task SyncTypesFromApiAsync()
    {
        try
        {
            var session = await _authService.GetCurrentSessionAsync();
            if (session == null) return;

            var token = await _authService.EnsureValidTokenAsync();
            if (token == null) return;

            var client = ApiClient.CreateAuthorized(_httpClientFactory, token);
            var response = await client.GetAsync($"api/typetickets/hammam/{session.HammamId}");
            if (!response.IsSuccessStatusCode) return;

            var apiTypes = await response.Content.ReadFromJsonAsync<List<ApiTypeTicket>>();
            if (apiTypes == null || apiTypes.Count == 0) return;

            var imageDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HammamPOS", "images", "typetickets");
            System.IO.Directory.CreateDirectory(imageDir);

            // Télécharger les images AVANT de toucher à la base (la table n'est jamais vide)
            var newTypes = new List<LocalTypeTicket>();
            foreach (var apiType in apiTypes)
            {
                newTypes.Add(new LocalTypeTicket
                {
                    Id = apiType.Id,
                    Nom = apiType.Nom,
                    Prix = apiType.Prix,
                    Couleur = apiType.Couleur ?? "#3B82F6",
                    Icone = apiType.Icone ?? "User",
                    ImageUrl = apiType.ImageUrl,
                    LocalImagePath = await DownloadImageAsync(client, apiType, imageDir),
                    Ordre = apiType.Ordre
                });
            }

            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.TypeTickets.RemoveRange(await db.TypeTickets.ToListAsync());
            await db.SaveChangesAsync();
            db.TypeTickets.AddRange(newTypes);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            Serilog.Log.Information("Types de tickets synchronisés depuis l'API: {Count}", newTypes.Count);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation des types de tickets");
        }
    }

    private static async Task<string?> DownloadImageAsync(HttpClient client, ApiTypeTicket apiType, string imageDir)
    {
        if (string.IsNullOrEmpty(apiType.ImageUrl))
            return null;

        try
        {
            Uri? imageUri = Uri.TryCreate(apiType.ImageUrl, UriKind.Absolute, out var absoluteUri)
                ? absoluteUri
                : client.BaseAddress != null ? new Uri(client.BaseAddress, apiType.ImageUrl) : null;

            if (imageUri == null)
                return null;

            var imageResponse = await client.GetAsync(imageUri);
            if (!imageResponse.IsSuccessStatusCode)
                return null;

            var ext = System.IO.Path.GetExtension(imageUri.AbsolutePath);
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            var localPath = System.IO.Path.Combine(imageDir, $"{apiType.Id}{ext}");

            await System.IO.File.WriteAllBytesAsync(localPath, await imageResponse.Content.ReadAsByteArrayAsync());
            return localPath;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Impossible de télécharger l'image pour {TypeName}", apiType.Nom);
            return null;
        }
    }

    private static List<LocalTypeTicket> CreateDefaultTypes() => new()
    {
        new() { Id = Guid.NewGuid(), Nom = "HOMME", Prix = 15, Couleur = "#3B82F6", Icone = "User", Ordre = 1 },
        new() { Id = Guid.NewGuid(), Nom = "FEMME", Prix = 15, Couleur = "#EC4899", Icone = "UserCheck", Ordre = 2 },
        new() { Id = Guid.NewGuid(), Nom = "ENFANT", Prix = 10, Couleur = "#22C55E", Icone = "Baby", Ordre = 3 },
        new() { Id = Guid.NewGuid(), Nom = "DOUCHE", Prix = 5, Couleur = "#06B6D4", Icone = "Droplets", Ordre = 4 }
    };

    private SemaphoreSlim GetHammamLock(Guid hammamId) =>
        _hammamLocks.GetOrAdd(hammamId, _ => new SemaphoreSlim(1, 1));

    public async Task<string> CreateTicketWithNextNumberAsync(LocalTicket ticket, int hammamPrefixeTicket)
    {
        var hammamLock = GetHammamLock(ticket.HammamId);
        await hammamLock.WaitAsync();

        try
        {
            // Appel réseau éventuel (calage sur le compteur serveur) AVANT la transaction :
            // une transaction SQLite verrouille la base en écriture, la synchro ne doit pas attendre le réseau.
            await SeedTicketCounterAsync(ticket.HammamId, hammamPrefixeTicket);

            const int maxAttempts = 5;

            for (var attempt = 1; ; attempt++)
            {
                // Contexte neuf à chaque tentative : aucun état résiduel après une collision
                await using var db = await _dbFactory.CreateDbContextAsync();
                try
                {
                    await using var transaction = await db.Database.BeginTransactionAsync();

                    var ticketNumber = await ReserveNextTicketNumberAsync(db, ticket.HammamId, hammamPrefixeTicket);
                    ticket.TicketNumber = ticketNumber;

                    db.Tickets.Add(ticket);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return ticketNumber;
                }
                catch (DbUpdateException ex) when (IsTicketNumberUniqueCollision(ex) && attempt < maxAttempts)
                {
                    Serilog.Log.Warning(ex,
                        "Collision numéro ticket détectée (tentative {Attempt}/{Max}), retry automatique",
                        attempt, maxAttempts);
                }
            }
        }
        finally
        {
            hammamLock.Release();
        }
    }

    private async Task SeedTicketCounterAsync(Guid hammamId, int hammamPrefixeTicket)
    {
        var configKey = $"{LastTicketConfigPrefix}{hammamId}";
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Configs.AnyAsync(c => c.Key == configKey))
            return;

        var snapshot = await GetLastTicketNumberSnapshotAsync(db, hammamId, hammamPrefixeTicket);
        db.Configs.Add(new LocalConfig { Key = configKey, Value = snapshot.ToString(), UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public async Task<string> GetNextTicketNumberAsync(Guid hammamId, int hammamPrefixeTicket)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var lastTicketNumber = await GetLastTicketNumberSnapshotAsync(db, hammamId, hammamPrefixeTicket);
        var deviceSuffix = await GetOrCreateDeviceSuffixAsync(db);
        return $"{lastTicketNumber + 1}-{deviceSuffix}";
    }

    public async Task<(int Count, decimal Revenue)> GetTodayStatsAsync(Guid hammamId)
    {
        // Bornes du jour local (Maroc, UTC+1) exprimées en UTC
        var todayUtcStart = (DateTime.UtcNow + MoroccoOffset).Date - MoroccoOffset;
        var tomorrowUtcStart = todayUtcStart.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var prices = await db.Tickets
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= todayUtcStart && t.CreatedAt < tomorrowUtcStart)
            .Select(t => t.Prix)
            .ToListAsync();

        if (prices.Count == 0 && _connectivityService.IsOnline)
        {
            var serverCount = await GetCountFromServerAsync($"api/tickets/count/today?hammamId={hammamId}");
            if (serverCount > 0)
            {
                Serilog.Log.Information("Compteur local vide, utilisation du compteur serveur: {Count}", serverCount);
                return (serverCount, 0);
            }
        }

        return (prices.Count, prices.Sum());
    }

    private async Task<int> GetCountFromServerAsync(string url)
    {
        try
        {
            var token = await _authService.EnsureValidTokenAsync();
            if (token == null) return 0;

            var client = ApiClient.CreateAuthorized(_httpClientFactory, token);
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Erreur lors de la récupération d'un compteur serveur ({Url})", url);
        }

        return 0;
    }

    private async Task<int> GetLastTicketNumberSnapshotAsync(LocalDbContext db, Guid hammamId, int hammamPrefixeTicket)
    {
        var configKey = $"{LastTicketConfigPrefix}{hammamId}";
        var config = await db.Configs.AsNoTracking().FirstOrDefaultAsync(c => c.Key == configKey);

        var lastTicketFromConfig = 0;
        if (config != null)
        {
            _ = int.TryParse(config.Value, out lastTicketFromConfig);
        }

        var localCount = await db.Tickets.CountAsync(t => t.HammamId == hammamId);
        var baseTicketNumber = hammamPrefixeTicket + localCount;

        // Premier ticket sur ce poste : se caler sur le compteur serveur si disponible.
        // IsOnline (dernier état connu) → aucun appel réseau bloquant quand on est hors ligne.
        if (config == null && _connectivityService.IsOnline)
        {
            var serverCount = await GetCountFromServerAsync($"api/tickets/count/total?hammamId={hammamId}");
            baseTicketNumber = hammamPrefixeTicket + Math.Max(localCount, serverCount);
        }

        return Math.Max(lastTicketFromConfig, baseTicketNumber);
    }

    private async Task<string> ReserveNextTicketNumberAsync(LocalDbContext db, Guid hammamId, int hammamPrefixeTicket)
    {
        var configKey = $"{LastTicketConfigPrefix}{hammamId}";
        var nextTicketNumber = await GetLastTicketNumberSnapshotAsync(db, hammamId, hammamPrefixeTicket) + 1;
        var deviceSuffix = await GetOrCreateDeviceSuffixAsync(db);

        var config = await db.Configs.FirstOrDefaultAsync(c => c.Key == configKey);
        if (config == null)
        {
            db.Configs.Add(new LocalConfig
            {
                Key = configKey,
                Value = nextTicketNumber.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            config.Value = nextTicketNumber.ToString();
            config.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return $"{nextTicketNumber}-{deviceSuffix}";
    }

    private static async Task<string> GetOrCreateDeviceSuffixAsync(LocalDbContext db)
    {
        var existing = await db.Configs.FirstOrDefaultAsync(c => c.Key == DeviceSuffixConfigKey);
        if (existing != null && IsValidDeviceSuffix(existing.Value))
        {
            return existing.Value;
        }

        var suffix = CreateRandomDeviceSuffix();

        if (existing == null)
        {
            db.Configs.Add(new LocalConfig { Key = DeviceSuffixConfigKey, Value = suffix, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            existing.Value = suffix;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return suffix;
    }

    private static bool IsTicketNumberUniqueCollision(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_tickets_hammam_ticket_number", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE constraint failed: tickets.HammamId, tickets.TicketNumber", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidDeviceSuffix(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 4 && value.All(char.IsLetterOrDigit);

    private static string CreateRandomDeviceSuffix()
    {
        Span<byte> bytes = stackalloc byte[3];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Tickets en attente. Les tickets déjà refusés passent APRÈS les nouveaux
    /// (tri par nombre de tentatives) : un ticket bloqué ne bloque plus la file.
    /// </summary>
    public async Task<List<LocalTicket>> GetUnsyncedTicketsAsync(int limit = 100)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Tickets
            .AsNoTracking()
            .Where(t => t.SyncStatus == StatusPending)
            .OrderBy(t => t.SyncAttempts)
            .ThenBy(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task MarkAsSyncedAsync(IReadOnlyCollection<Guid> ticketIds)
    {
        if (ticketIds.Count == 0) return;

        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.SyncStatus, StatusSynced)
                .SetProperty(t => t.SyncedAt, now)
                .SetProperty(t => t.LastSyncError, (string?)null));
    }

    public async Task MarkSyncFailedAsync(IReadOnlyCollection<Guid> ticketIds, string reason)
    {
        if (ticketIds.Count == 0) return;

        var error = reason.Length > 500 ? reason[..500] : reason;
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.SyncAttempts, t => t.SyncAttempts + 1)
                .SetProperty(t => t.LastSyncError, error));
    }

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
/// Synchronisation automatique avec le serveur :
/// - au retour de la connexion, après chaque vente et toutes les N minutes ;
/// - envoie TOUS les tickets en attente par lots successifs (plus besoin de cliquer en boucle) ;
/// - un ticket refusé est reporté en fin de file au lieu de bloquer les autres.
/// </summary>
public class SyncService : ISyncService, IDisposable
{
    private const int MaxBatchesPerRun = 200;

    private readonly ITicketService _ticketService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly IAuthService _authService;
    private readonly IDbContextFactory<LocalDbContext> _dbFactory;
    private readonly SyncSettings _settings;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private Timer? _syncTimer;

    public event EventHandler<SyncEventArgs>? SyncCompleted;

    public SyncService(
        ITicketService ticketService,
        IHttpClientFactory httpClientFactory,
        IConnectivityService connectivityService,
        IAuthService authService,
        IDbContextFactory<LocalDbContext> dbFactory,
        IOptions<SyncSettings> settings)
    {
        _ticketService = ticketService;
        _httpClientFactory = httpClientFactory;
        _connectivityService = connectivityService;
        _authService = authService;
        _dbFactory = dbFactory;
        _settings = settings.Value;
    }

    private int BatchSize => Math.Clamp(_settings.BatchSize, 1, 200);

    public void StartBackgroundSync()
    {
        if (_syncTimer != null) return;

        _connectivityService.ConnectivityChanged += OnConnectivityChanged;

        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
        _syncTimer = new Timer(_ => _ = TrySyncAsync(), null, TimeSpan.FromSeconds(5), interval);

        Serilog.Log.Information(
            "Synchronisation automatique démarrée (toutes les {Minutes} minutes + au retour de connexion)",
            interval.TotalMinutes);
    }

    private void OnConnectivityChanged(object? sender, bool isOnline)
    {
        if (!isOnline) return;

        Serilog.Log.Information("Connexion détectée — synchronisation automatique des tickets en attente");
        _ = TrySyncAsync();
    }

    public void RequestSync()
    {
        if (_connectivityService.IsOnline)
        {
            _ = TrySyncAsync();
        }
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

            await RunSyncAsync(waitIfRunning: false);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation automatique");
        }
    }

    /// <summary>Bouton "Synchroniser" : si une synchro tourne déjà, on l'attend puis on relance.</summary>
    public Task SyncNowAsync() => RunSyncAsync(waitIfRunning: true);

    private async Task RunSyncAsync(bool waitIfRunning)
    {
        if (!await _syncLock.WaitAsync(waitIfRunning ? TimeSpan.FromMinutes(3) : TimeSpan.Zero))
        {
            Serilog.Log.Debug("Synchronisation déjà en cours, ignorée");
            return;
        }

        var totalSynced = 0;
        var totalErrors = 0;
        var attempted = false;
        var completedNormally = true;
        var sentThisRun = new HashSet<Guid>();

        try
        {
            for (var batch = 0; batch < MaxBatchesPerRun; batch++)
            {
                var tickets = (await _ticketService.GetUnsyncedTicketsAsync(BatchSize))
                    .Where(t => !sentThisRun.Contains(t.Id))
                    .ToList();

                // Vide, ou il ne reste que des tickets déjà refusés pendant ce passage
                if (tickets.Count == 0)
                {
                    if (batch == 0) Serilog.Log.Debug("Aucun ticket à synchroniser");
                    break;
                }

                var token = await _authService.GetSyncTokenAsync();
                if (token == null)
                {
                    Serilog.Log.Warning("Pas de token valide pour la synchronisation, report (tickets conservés en local)");
                    completedNormally = false;
                    break;
                }

                attempted = true;
                Serilog.Log.Information("Synchronisation de {Count} tickets...", tickets.Count);

                var outcome = await SendBatchAsync(token, tickets);
                if (outcome == null)
                {
                    // Erreur serveur/réseau : on ne pénalise pas les tickets, on réessaiera plus tard
                    completedNormally = false;
                    break;
                }

                var (successIds, failedIds) = outcome.Value;
                sentThisRun.UnionWith(failedIds);
                await _ticketService.MarkAsSyncedAsync(successIds);
                await _ticketService.MarkSyncFailedAsync(failedIds, "Refusé par le serveur");

                totalSynced += successIds.Count;
                totalErrors += failedIds.Count;

                Serilog.Log.Information(
                    "Synchronisation terminée: {Synced} réussis, {Errors} échoués sur {Total} total",
                    successIds.Count, failedIds.Count, tickets.Count);

            }

            // Garder les accès hors ligne à jour (limité à une fois toutes les 30 min)
            await _authService.RefreshOfflineCredentialsAsync();
        }
        catch (Exception ex)
        {
            completedNormally = false;
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation");
        }
        finally
        {
            _syncLock.Release();
        }

        if (attempted)
        {
            SyncCompleted?.Invoke(this, new SyncEventArgs
            {
                SyncedCount = totalSynced,
                ErrorCount = totalErrors,
                PendingCount = await GetPendingCountSafeAsync(),
                Success = completedNormally && totalErrors == 0
            });
        }
    }

    /// <summary>
    /// Envoie un lot. Renvoie null si le lot n'a pas pu être traité (serveur/réseau),
    /// sinon les tickets acceptés et refusés.
    /// </summary>
    private async Task<(List<Guid> Success, List<Guid> Failed)?> SendBatchAsync(string token, List<LocalTicket> tickets)
    {
        try
        {
            var response = await PostAsync(token, tickets);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await _authService.GetSyncTokenAsync(forceRefresh: true);
                if (refreshed == null)
                {
                    Serilog.Log.Warning("Token refusé (401) et ré-authentification impossible");
                    return null;
                }

                token = refreshed;
                response = await PostAsync(token, tickets);
            }

            // Un ticket malformé fait rejeter tout le lot (400) : on isole le coupable ticket par ticket
            if (response.StatusCode == HttpStatusCode.BadRequest && tickets.Count > 1)
            {
                Serilog.Log.Warning("Lot refusé (400), envoi ticket par ticket pour isoler le ticket invalide");
                return await SendOneByOneAsync(token, tickets);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (new List<Guid>(), tickets.Select(t => t.Id).ToList());
            }

            if (!response.IsSuccessStatusCode)
            {
                Serilog.Log.Warning("Échec de la synchronisation: {StatusCode}", response.StatusCode);
                return null;
            }

            var syncResult = await response.Content.ReadFromJsonAsync<SyncResponse>();
            var failed = syncResult?.FailedTicketIds?.ToHashSet() ?? new HashSet<Guid>();

            return (
                tickets.Where(t => !failed.Contains(t.Id)).Select(t => t.Id).ToList(),
                tickets.Where(t => failed.Contains(t.Id)).Select(t => t.Id).ToList());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Serilog.Log.Warning(ex, "Serveur injoignable pendant la synchronisation, report");
            return null;
        }
    }

    private async Task<(List<Guid> Success, List<Guid> Failed)?> SendOneByOneAsync(string token, List<LocalTicket> tickets)
    {
        var success = new List<Guid>();
        var failed = new List<Guid>();

        foreach (var ticket in tickets)
        {
            var result = await SendBatchAsync(token, new List<LocalTicket> { ticket });
            if (result == null)
                return success.Count + failed.Count > 0 ? (success, failed) : null;

            success.AddRange(result.Value.Success);
            failed.AddRange(result.Value.Failed);
        }

        return (success, failed);
    }

    private Task<HttpResponseMessage> PostAsync(string token, List<LocalTicket> tickets)
    {
        var client = ApiClient.CreateAuthorized(_httpClientFactory, token);
        return client.PostAsJsonAsync("api/tickets/sync", new
        {
            Tickets = tickets.Select(t => new
            {
                t.Id,
                TicketNumber = string.IsNullOrWhiteSpace(t.TicketNumber)
                    ? $"LEGACY-{t.Id:N}"
                    : t.TicketNumber,
                t.TypeTicketId,
                t.EmployeId,
                t.HammamId,
                t.Prix,
                t.CreatedAt,
                t.DeviceId
            })
        });
    }

    private record SyncResponse(
        int TotalReceived,
        int Inserted,
        int Updated,
        int Errors,
        IEnumerable<Guid>? FailedTicketIds
    );

    public async Task<int> GetPendingCountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Tickets.CountAsync(t => t.SyncStatus == "Pending");
    }

    private async Task<int> GetPendingCountSafeAsync()
    {
        try
        {
            return await GetPendingCountAsync();
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
        _syncTimer?.Dispose();
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
/// Détection de connectivité : vérification périodique (toutes les 15 s) et exposition du dernier
/// état connu (<see cref="IsOnline"/>) sans appel réseau bloquant.
/// </summary>
public class ConnectivityService : IConnectivityService, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Timer _timer; // gardé en champ : sinon ramassé par le GC et la détection s'arrête
    private readonly object _stateLock = new();
    private bool _lastState;
    private int _periodicCheckRunning;

    public event EventHandler<bool>? ConnectivityChanged;

    public bool IsOnline
    {
        get { lock (_stateLock) return _lastState; }
    }

    public ConnectivityService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _timer = new Timer(_ => _ = PeriodicCheckAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15));
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        bool isOnline;
        try
        {
            var client = _httpClientFactory.CreateClient("HammamApiFast");
            using var response = await client.GetAsync("health");
            isOnline = response.IsSuccessStatusCode;
        }
        catch
        {
            isOnline = false;
        }

        UpdateState(isOnline);
        return isOnline;
    }

    private async Task PeriodicCheckAsync()
    {
        if (Interlocked.Exchange(ref _periodicCheckRunning, 1) == 1)
            return;

        try
        {
            await CheckConnectivityAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _periodicCheckRunning, 0);
        }
    }

    private void UpdateState(bool isOnline)
    {
        lock (_stateLock)
        {
            if (_lastState == isOnline)
                return;
            _lastState = isOnline;
        }

        Serilog.Log.Information("Changement de connectivité: {State}", isOnline ? "En ligne" : "Hors ligne");

        try
        {
            ConnectivityChanged?.Invoke(this, isOnline);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur dans un abonné ConnectivityChanged");
        }
    }

    public void Dispose() => _timer.Dispose();
}

#endregion
