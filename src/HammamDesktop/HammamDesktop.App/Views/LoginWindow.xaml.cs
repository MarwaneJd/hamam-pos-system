using MaterialDesignThemes.Wpf;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HammamDesktop.ViewModels;

namespace HammamDesktop.Views;

/// <summary>
/// Fenêtre de connexion avec sélection de profil
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly IHttpClientFactory _httpClientFactory;
    private List<EmployeProfile> _profiles = new();
    private EmployeProfile? _selectedProfile;

    public LoginWindow(LoginViewModel viewModel, IHttpClientFactory httpClientFactory)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            _httpClientFactory = httpClientFactory;
            DataContext = _viewModel;

            Loaded += LoginWindow_Loaded;
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
                return;
            }

            // Charger les profils depuis l'API
            await LoadProfilesAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors du chargement");
        }
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            LoadingProfilesText.Visibility = Visibility.Visible;
            ProfilesGrid.Children.Clear();

            var client = _httpClientFactory.CreateClient("HammamApi");
            var profiles = await client.GetFromJsonAsync<List<EmployeProfile>>("api/auth/profiles");

            if (profiles == null || !profiles.Any())
            {
                LoadingProfilesText.Text = "Aucun profil trouvé";
                return;
            }

            _profiles = profiles;
            LoadingProfilesText.Visibility = Visibility.Collapsed;

            // Ne pas afficher le nom du hammam - la connexion est ouverte pour tous
            HammamNameText.Text = "";

            // Créer les cartes de profil
            foreach (var profile in profiles)
            {
                var card = CreateProfileCard(profile);
                ProfilesGrid.Children.Add(card);
            }
        }
        catch (HttpRequestException)
        {
            LoadingProfilesText.Text = "Impossible de joindre le serveur";
            Serilog.Log.Warning("Impossible de charger les profils - serveur inaccessible");
        }
        catch (Exception ex)
        {
            LoadingProfilesText.Text = "Erreur de chargement";
            Serilog.Log.Error(ex, "Erreur lors du chargement des profils");
        }
    }

    private Border CreateProfileCard(EmployeProfile profile)
    {
        // Couleur basée sur l'icône (2 couleurs uniquement)
        var color = profile.Icone switch
        {
            "User1" => "#3B82F6", // Bleu
            "User2" => "#10B981", // Vert
            _ => "#3B82F6"
        };

        var iconKind = profile.Icone switch
        {
            "User1" => PackIconKind.Account,
            "User2" => PackIconKind.AccountCircle,
            _ => PackIconKind.Account
        };

        var card = new Border
        {
            Width = 140,
            Height = 160,
            Margin = new Thickness(10),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")!),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")!),
            BorderThickness = new Thickness(2),
            Cursor = Cursors.Hand,
            Tag = profile
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Cercle avec icône
        var iconBorder = new Border
        {
            Width = 80,
            Height = 80,
            CornerRadius = new CornerRadius(40),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var icon = new PackIcon
        {
            Kind = iconKind,
            Width = 50,
            Height = 50,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        iconBorder.Child = icon;
        stack.Children.Add(iconBorder);

        // Nom d'utilisateur (Utilisateur1, Utilisateur2, etc.)
        var nameText = new TextBlock
        {
            Text = profile.Username,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")!),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        stack.Children.Add(nameText);

        card.Child = stack;

        // Événement de clic
        card.MouseLeftButtonUp += (s, e) => SelectProfile(profile, color, iconKind);

        // Effet au survol
        card.MouseEnter += (s, e) =>
        {
            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")!);
        };
        card.MouseLeave += (s, e) =>
        {
            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")!);
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")!);
        };

        return card;
    }

    private void SelectProfile(EmployeProfile profile, string color, PackIconKind iconKind)
    {
        _selectedProfile = profile;
        _viewModel.Username = profile.Username;

        // Mettre à jour l'affichage avec le username (Utilisateur1, Utilisateur2, etc.)
        SelectedProfileName.Text = profile.Username;
        SelectedProfileBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        SelectedProfileIcon.Kind = iconKind;

        // Passer à l'écran de mot de passe
        ProfileSelectionPanel.Visibility = Visibility.Collapsed;
        PasswordPanel.Visibility = Visibility.Visible;

        // Focus sur le champ mot de passe
        PasswordBox.Focus();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Retour à la sélection de profil
        _selectedProfile = null;
        PasswordBox.Password = "";
        ErrorText.Visibility = Visibility.Collapsed;

        PasswordPanel.Visibility = Visibility.Collapsed;
        ProfileSelectionPanel.Visibility = Visibility.Visible;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile == null) return;

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
            ErrorText.Text = "Erreur de connexion";
            ErrorText.Visibility = Visibility.Visible;
            Serilog.Log.Error(ex, "Erreur lors de la connexion");
        }
        finally
        {
            LoadingText.Visibility = Visibility.Collapsed;
        }
    }
}

/// <summary>
/// Profil employé pour l'affichage
/// </summary>
public class EmployeProfile
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Icone { get; set; } = "User1";
    public Guid HammamId { get; set; }
    public string HammamNom { get; set; } = "";
}
