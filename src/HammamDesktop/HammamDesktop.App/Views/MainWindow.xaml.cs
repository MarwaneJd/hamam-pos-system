using System.Windows;
using System.Windows.Threading;
using HammamDesktop.ViewModels;

namespace HammamDesktop.Views;

/// <summary>
/// Fenêtre principale de vente
/// </summary>
public partial class MainWindow : Window
{
    private readonly SalesViewModel _viewModel;
    private readonly DispatcherTimer _clockTimer;

    public MainWindow(SalesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Timer pour l'horloge
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        UpdateClock();
    }

    private void UpdateClock()
    {
        TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    protected override async void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        
        // Déconnexion automatique à la fermeture (supprimer la session)
        try
        {
            var authService = (Services.AuthService)App.GetService<Services.IAuthService>();
            await authService.ClearSessionAsync();
        }
        catch { }
        
        base.OnClosed(e);
    }
}
