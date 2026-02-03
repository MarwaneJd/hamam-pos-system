using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HammamDesktop.Services;
using HammamDesktop.Data.Entities;

namespace HammamDesktop.ViewModels;

/// <summary>
/// ViewModel principal pour la fenêtre principale après connexion
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ITicketService _ticketService;
    private readonly ISyncService _syncService;
    private readonly IConnectivityService _connectivityService;

    private LocalSession? _currentSession;

    [ObservableProperty]
    private string _employeNom = "";

    [ObservableProperty]
    private string _hammamNom = "";

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private int _pendingSync;

    [ObservableProperty]
    private int _ticketsToday;

    [ObservableProperty]
    private decimal _revenueToday;

    [ObservableProperty]
    private string _currentTime = "";

    public MainViewModel(
        IAuthService authService,
        ITicketService ticketService,
        ISyncService syncService,
        IConnectivityService connectivityService)
    {
        _authService = authService;
        _ticketService = ticketService;
        _syncService = syncService;
        _connectivityService = connectivityService;
    }

    public async Task InitializeAsync()
    {
        // Récupérer les infos de session
        _currentSession = await _authService.GetCurrentSessionAsync();
        if (_currentSession != null)
        {
            EmployeNom = $"{_currentSession.Prenom} {_currentSession.Nom}";
            HammamNom = _currentSession.HammamNom;
        }

        // État de connectivité
        IsOnline = await _connectivityService.CheckConnectivityAsync();
        _connectivityService.ConnectivityChanged += (_, connected) => IsOnline = connected;

        // Mettre à jour l'heure
        UpdateTime();
        
        // Charger les stats
        await RefreshStatsAsync();
    }

    public void UpdateTime()
    {
        CurrentTime = DateTime.Now.ToString("HH:mm:ss");
    }

    public async Task RefreshStatsAsync()
    {
        if (_currentSession == null) return;
        
        var stats = await _ticketService.GetTodayStatsAsync(_currentSession.EmployeId);
        TicketsToday = stats.Count;
        RevenueToday = stats.Revenue;
        PendingSync = await _syncService.GetPendingCountAsync();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        await _syncService.SyncNowAsync();
        await RefreshStatsAsync();
    }
}
