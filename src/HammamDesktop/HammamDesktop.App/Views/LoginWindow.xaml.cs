using System.Windows;
using HammamDesktop.ViewModels;

namespace HammamDesktop.Views;

/// <summary>
/// Fenêtre de connexion
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += LoginWindow_Loaded;
            UsernameBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'initialisation de la fenêtre: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Vérifier s'il y a une session existante valide
            if (await _viewModel.CheckExistingSessionAsync())
            {
                // Session valide, ouvrir directement l'app
                var mainWindow = App.GetService<MainWindow>();
                mainWindow.Show();
                Close();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors de la vérification de session");
        }
    }

    /// <summary>
    /// Synchroniser le mot de passe avec le ViewModel
    /// (PasswordBox ne supporte pas le binding direct pour des raisons de sécurité)
    /// </summary>
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
    }

    /// <summary>
    /// Bouton de connexion
    /// </summary>
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorText.Visibility = Visibility.Collapsed;
            LoadingText.Visibility = Visibility.Visible;

            await _viewModel.LoginCommand.ExecuteAsync(null);

            if (_viewModel.HasError)
            {
                ErrorText.Text = _viewModel.ErrorMessage;
                ErrorText.Visibility = Visibility.Visible;
            }
            else
            {
                // Connexion réussie, ouvrir la fenêtre principale
                var mainWindow = App.GetService<MainWindow>();
                mainWindow.Show();
                Close();
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Erreur: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingText.Visibility = Visibility.Collapsed;
        }
    }
}
