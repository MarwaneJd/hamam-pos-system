using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HammamDesktop.Data;
using HammamDesktop.Data.Entities;
using HammamDesktop.Services;
using System.Collections.ObjectModel;

namespace HammamDesktop.ViewModels;

/// <summary>
/// ViewModel principal pour l'écran de vente
/// </summary>
public partial class SalesViewModel : ObservableObject
{
    private readonly ITicketService _ticketService;
    private readonly IAudioService _audioService;
    private readonly ISyncService _syncService;
    private readonly IConnectivityService _connectivityService;
    private readonly IAuthService _authService;
    private readonly IPrintService _printService;

    [ObservableProperty]
    private ObservableCollection<TicketTypeViewModel> _ticketTypes = new();

    [ObservableProperty]
    private int _todayTicketsCount;

    [ObservableProperty]
    private decimal _todayRevenue;

    [ObservableProperty]
    private int _nextTicketNumber;

    [ObservableProperty]
    private string _todayDate = string.Empty;

    [ObservableProperty]
    private int _pendingSyncCount;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private string _employeNom = string.Empty;

    [ObservableProperty]
    private string _hammamNom = string.Empty;

    [ObservableProperty]
    private string _hammamNomArabe = string.Empty;

    private int _hammamPrefixeTicket = 100000;
    private int _totalTicketCount = 0; // Compteur total permanent (ne se réinitialise jamais)

    [ObservableProperty]
    private string _lastSaleMessage = string.Empty;

    [ObservableProperty]
    private bool _showConfirmation;

    public SalesViewModel(
        ITicketService ticketService,
        IAudioService audioService,
        ISyncService syncService,
        IConnectivityService connectivityService,
        IAuthService authService,
        IPrintService printService)
    {
        _ticketService = ticketService;
        _audioService = audioService;
        _syncService = syncService;
        _connectivityService = connectivityService;
        _authService = authService;
        _printService = printService;

        // S'abonner aux changements de connectivité
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
    }

    /// <summary>
    /// Initialiser les données au chargement
    /// </summary>
    public async Task InitializeAsync()
    {
        // Charger les infos de session
        var session = await _authService.GetCurrentSessionAsync();
        if (session != null)
        {
            EmployeNom = $"{session.Prenom} {session.Nom}";
            HammamNom = session.HammamNom;
            HammamNomArabe = session.HammamNomArabe;
            _hammamPrefixeTicket = session.HammamPrefixeTicket;
        }

        // Charger les types de tickets
        var types = await _ticketService.GetTicketTypesAsync();
        TicketTypes.Clear();
        foreach (var type in types)
        {
            TicketTypes.Add(new TicketTypeViewModel
            {
                Id = type.Id,
                Nom = type.Nom,
                Prix = type.Prix,
                Couleur = type.Couleur,
                Icone = type.Icone ?? "User",
                LocalImagePath = type.LocalImagePath
            });
        }

        // Mettre à jour les compteurs
        await RefreshCountersAsync();

        // Date d'aujourd'hui
        TodayDate = DateTime.Now.ToString("dd/MM/yyyy");

        // État de la connexion
        IsOnline = await _connectivityService.CheckConnectivityAsync();
        PendingSyncCount = await _syncService.GetPendingCountAsync();
    }

    /// <summary>
    /// Vendre un ticket
    /// </summary>
    [RelayCommand]
    private async Task SellTicketAsync(TicketTypeViewModel ticketType)
    {
        try
        {
            var session = await _authService.GetCurrentSessionAsync();
            if (session == null)
            {
                // Session expirée, retour au login
                await _authService.LogoutAsync();
                return;
            }

            // Créer le ticket
            var ticket = new LocalTicket
            {
                Id = Guid.NewGuid(), // UUID généré localement
                TypeTicketId = ticketType.Id,
                TypeTicketNom = ticketType.Nom,
                EmployeId = session.EmployeId,
                HammamId = session.HammamId,
                Prix = ticketType.Prix,
                CreatedAt = DateTime.UtcNow,
                SyncStatus = "Pending",
                DeviceId = Environment.MachineName
            };

            await _ticketService.CreateTicketAsync(ticket);

            // Jouer le son de confirmation
            await _audioService.PlayBeepAsync();

            // 🖨️ IMPRESSION AUTOMATIQUE DU TICKET
            var ticketNumber = _hammamPrefixeTicket + _totalTicketCount + 1; // Préfixe + compteur permanent
            await _printService.PrintTicketAsync(new TicketPrintData
            {
                HammamNom = HammamNom,
                HammamNomArabe = HammamNomArabe,
                TicketNumber = ticketNumber,
                TypeTicket = ticketType.Nom,
                Prix = ticketType.Prix,
                DateHeure = DateTime.Now,
                EmployeNom = EmployeNom,
                Couleur = ticketType.Couleur,
                TypeTicketImagePath = ticketType.LocalImagePath
            });

            // Afficher la confirmation
            LastSaleMessage = $"{ticketType.Nom} - {ticketType.Prix} DH";
            ShowConfirmation = true;

            // Cacher après 0.8 seconde (rapide pour ne pas gêner)
            _ = Task.Delay(800).ContinueWith(_ =>
            {
                ShowConfirmation = false;
            });

            // Mettre à jour les compteurs
            await RefreshCountersAsync();

            Serilog.Log.Information(
                "Ticket vendu et imprimé: #{TicketNumber} {TicketType} - {Prix} DH par {Employe}",
                ticketNumber, ticketType.Nom, ticketType.Prix, session.Username);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la vente du ticket");
        }
    }

    /// <summary>
    /// Forcer la synchronisation
    /// </summary>
    [RelayCommand]
    private async Task ForceSyncAsync()
    {
        if (!IsOnline) return;

        try
        {
            await _syncService.SyncNowAsync();
            PendingSyncCount = await _syncService.GetPendingCountAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la synchronisation forcée");
        }
    }

    /// <summary>
    /// Déconnexion
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
    }

    /// <summary>
    /// Imprimer ticket de clôture
    /// </summary>
    [RelayCommand]
    private async Task PrintClotureAsync()
    {
        try
        {
            // Mapping des noms arabes (en dur pour éviter les problèmes d'encodage)
            var arabicName = HammamNom.ToLower() switch
            {
                "hammame liberte" => "حمام الحرية",
                "hammam centre" => "حمام الوسط",
                "hammam casablanca" => "حمام الدار البيضاء",
                "hammam lasseif" => "حمام لاسميف",
                "hammam lassif" => "حمام لاسميف",
                "hammam lassmif" => "حمام لاسميف",
                _ => HammamNomArabe
            };

            await _printService.PrintClotureTicketAsync(new ClotureTicketData
            {
                HammamNomArabe = arabicName,
                HammamNom = HammamNom,
                CaissierNom = EmployeNom,
                DateHeure = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de l'impression du ticket de clôture");
        }
    }

    /// <summary>
    /// Rafraîchir les compteurs
    /// </summary>
    private async Task RefreshCountersAsync()
    {
        var session = await _authService.GetCurrentSessionAsync();
        if (session == null) return;

        var stats = await _ticketService.GetTodayStatsAsync(session.HammamId);
        TodayTicketsCount = stats.Count;
        TodayRevenue = stats.Revenue;

        // Compteur permanent : total de TOUS les tickets du hammam (jamais remis à zéro)
        _totalTicketCount = await _ticketService.GetTotalTicketCountAsync(session.HammamId);
        NextTicketNumber = _hammamPrefixeTicket + _totalTicketCount + 1;

        PendingSyncCount = await _syncService.GetPendingCountAsync();
    }

    /// <summary>
    /// Callback changement de connectivité
    /// </summary>
    private void OnConnectivityChanged(object? sender, bool isOnline)
    {
        IsOnline = isOnline;
    }
}

/// <summary>
/// ViewModel pour un type de ticket
/// </summary>
public partial class TicketTypeViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public decimal Prix { get; set; }
    public string Couleur { get; set; } = "#3B82F6";
    public string Icone { get; set; } = "User"; // Nom de l'icône (User, UserCheck, Baby, Droplets)
    public string? LocalImagePath { get; set; } // Chemin local de l'image cachée
    public bool HasImage => !string.IsNullOrEmpty(LocalImagePath) && System.IO.File.Exists(LocalImagePath);
}
