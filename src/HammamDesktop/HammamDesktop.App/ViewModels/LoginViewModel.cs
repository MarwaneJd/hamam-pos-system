using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HammamDesktop.Services;
using System.Windows;

namespace HammamDesktop.ViewModels;

/// <summary>
/// ViewModel pour la page de connexion
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Vérifie si une session est déjà active
    /// </summary>
    public async Task<bool> CheckExistingSessionAsync()
    {
        return await _authService.HasValidSessionAsync();
    }

    /// <summary>
    /// Commande de connexion
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            HasError = true;
            ErrorMessage = "Veuillez remplir tous les champs";
            return;
        }

        HasError = false;
        IsLoading = true;

        try
        {
            var result = await _authService.LoginAsync(Username, Password);

            if (result.Success)
            {
                // Ouvrir la fenêtre principale
                var mainWindow = App.GetService<Views.MainWindow>();
                mainWindow.Show();

                // Fermer la fenêtre de login
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Identifiants invalides";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = "Erreur de connexion. Vérifiez votre connexion internet.";
            Serilog.Log.Error(ex, "Erreur lors de la connexion");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Efface le message d'erreur quand l'utilisateur tape
    /// </summary>
    partial void OnUsernameChanged(string value)
    {
        if (HasError) HasError = false;
    }

    partial void OnPasswordChanged(string value)
    {
        if (HasError) HasError = false;
    }
}
