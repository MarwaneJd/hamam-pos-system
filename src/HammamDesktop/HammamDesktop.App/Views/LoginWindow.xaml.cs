using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HammamDesktop.Data;
using HammamDesktop.Data.Entities;
using HammamDesktop.ViewModels;

namespace HammamDesktop.Views;

/// <summary>
/// Fenêtre de connexion avec sélection de profil
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LocalDbContext _db;
    private List<EmployeProfile> _profiles = new();
    private EmployeProfile? _selectedProfile;
    private DispatcherTimer? _autoLoginTimer;
    private bool _isAutoLogging;
    private bool _isOfflineMode;

    public LoginWindow(LoginViewModel viewModel, IHttpClientFactory httpClientFactory, LocalDbContext db)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            _httpClientFactory = httpClientFactory;
            _db = db;
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
            _isOfflineMode = false;
            LoadingProfilesText.Visibility = Visibility.Collapsed;

            // Ne pas afficher le nom du hammam - la connexion est ouverte pour tous
            HammamNameText.Text = "";

            // Créer les cartes de profil
            foreach (var profile in profiles)
            {
                var card = CreateProfileCard(profile);
                ProfilesGrid.Children.Add(card);
            }

            // Mettre en cache les profils pour utilisation hors ligne
            await CacheProfilesAsync(profiles);
        }
        catch (HttpRequestException)
        {
            Serilog.Log.Warning("Serveur inaccessible - tentative de chargement des profils en cache");
            await LoadCachedProfilesAsync();
        }
        catch (TaskCanceledException)
        {
            Serilog.Log.Warning("Timeout serveur - tentative de chargement des profils en cache");
            await LoadCachedProfilesAsync();
        }
        catch (Exception ex)
        {
            LoadingProfilesText.Text = "Erreur de chargement";
            Serilog.Log.Error(ex, "Erreur lors du chargement des profils");
        }
    }

    private async Task CacheProfilesAsync(List<EmployeProfile> profiles)
    {
        try
        {
            var existingProfiles = await _db.EmployeProfiles.ToListAsync();
            var serverIds = profiles.Select(p => p.Id).ToHashSet();

            // Supprimer les profils qui n'existent plus sur le serveur
            var toRemove = existingProfiles.Where(p => !serverIds.Contains(p.Id)).ToList();
            if (toRemove.Any())
                _db.EmployeProfiles.RemoveRange(toRemove);

            // Ajouter ou mettre à jour les profils
            foreach (var profile in profiles)
            {
                var existing = existingProfiles.FirstOrDefault(p => p.Id == profile.Id);
                if (existing != null)
                {
                    existing.Username = profile.Username;
                    existing.Prenom = profile.Prenom;
                    existing.Nom = profile.Nom;
                    existing.Icone = profile.Icone;
                    existing.HammamId = profile.HammamId;
                    existing.HammamNom = profile.HammamNom;
                    existing.CachedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.EmployeProfiles.Add(new LocalEmployeProfile
                    {
                        Id = profile.Id,
                        Username = profile.Username,
                        Prenom = profile.Prenom,
                        Nom = profile.Nom,
                        Icone = profile.Icone,
                        HammamId = profile.HammamId,
                        HammamNom = profile.HammamNom,
                        CachedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            Serilog.Log.Information("Profils mis en cache: {Count} profils", profiles.Count);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Impossible de mettre en cache les profils");
        }
    }

    private async Task LoadCachedProfilesAsync()
    {
        try
        {
            var cachedProfiles = await _db.EmployeProfiles.ToListAsync();

            if (!cachedProfiles.Any())
            {
                LoadingProfilesText.Text = "Impossible de joindre le serveur";
                return;
            }

            // Convertir les profils en cache vers le format attendu
            _profiles = cachedProfiles.Select(p => new EmployeProfile
            {
                Id = p.Id,
                Username = p.Username,
                Prenom = p.Prenom,
                Nom = p.Nom,
                Icone = p.Icone,
                HammamId = p.HammamId,
                HammamNom = p.HammamNom
            }).ToList();

            _isOfflineMode = true;
            LoadingProfilesText.Visibility = Visibility.Collapsed;
            HammamNameText.Text = "Mode hors ligne";
            HammamNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")!);

            // Identifier les profils qui ont un mot de passe en cache
            var profilesWithHash = cachedProfiles
                .Where(p => !string.IsNullOrEmpty(p.PasswordHash))
                .Select(p => p.Id)
                .ToHashSet();

            foreach (var profile in _profiles)
            {
                bool hasOfflineAccess = profilesWithHash.Contains(profile.Id);
                var card = CreateProfileCard(profile, hasOfflineAccess);
                ProfilesGrid.Children.Add(card);
            }

            Serilog.Log.Information("Profils chargés depuis le cache: {Count} profils", _profiles.Count);
        }
        catch (Exception ex)
        {
            LoadingProfilesText.Text = "Impossible de joindre le serveur";
            Serilog.Log.Error(ex, "Erreur lors du chargement des profils en cache");
        }
    }

    private Border CreateProfileCard(EmployeProfile profile, bool hasOfflineAccess = true)
    {
        // En mode hors ligne, griser les profils sans accès offline
        bool isDisabled = _isOfflineMode && !hasOfflineAccess;

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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDisabled ? "#F1F5F9" : "#F8FAFC")!),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDisabled ? "#CBD5E1" : "#E2E8F0")!),
            BorderThickness = new Thickness(2),
            Cursor = isDisabled ? Cursors.No : Cursors.Hand,
            Opacity = isDisabled ? 0.5 : 1.0,
            Tag = profile,
            ToolTip = isDisabled ? "Connectez-vous en ligne une première fois pour activer le mode hors ligne" : null
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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDisabled ? "#94A3B8" : color)!),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var icon = new PackIcon
        {
            Kind = isDisabled ? PackIconKind.LockOutline : iconKind,
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
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDisabled ? "#94A3B8" : "#1E293B")!),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        stack.Children.Add(nameText);

        card.Child = stack;

        if (!isDisabled)
        {
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
        }

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
        _autoLoginTimer?.Stop();

        PasswordPanel.Visibility = Visibility.Collapsed;
        ProfileSelectionPanel.Visibility = Visibility.Visible;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
        ErrorText.Visibility = Visibility.Collapsed;

        // Réinitialiser le timer auto-login à chaque frappe
        if (_autoLoginTimer != null)
        {
            _autoLoginTimer.Stop();
        }

        // Ne lancer le timer que si le champ n'est pas vide
        if (!string.IsNullOrEmpty(PasswordBox.Password))
        {
            _autoLoginTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _autoLoginTimer.Tick += AutoLoginTimer_Tick;
            _autoLoginTimer.Start();
        }
    }

    private async void AutoLoginTimer_Tick(object? sender, EventArgs e)
    {
        // Arrêter le timer (un seul essai)
        _autoLoginTimer?.Stop();

        if (_selectedProfile == null || _isAutoLogging) return;
        if (string.IsNullOrEmpty(PasswordBox.Password)) return;

        _isAutoLogging = true;

        try
        {
            // Tenter la connexion silencieusement
            await _viewModel.LoginCommand.ExecuteAsync(null);

            // Si erreur → ne rien afficher, l'utilisateur continue de taper
            // Si succès → le ViewModel gère la navigation
        }
        catch
        {
            // Silencieux - pas de message d'erreur
        }
        finally
        {
            _isAutoLogging = false;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            LoginButton_Click(sender, e);
        }
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
