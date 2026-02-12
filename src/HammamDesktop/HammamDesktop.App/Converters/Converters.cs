using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HammamDesktop.Converters;

/// <summary>
/// Convertit un booléen en Visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// Convertit un booléen inversé en Visibility
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Collapsed;
    }
}

/// <summary>
/// Convertit un int > 0 en Visibility
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit un booléen en couleur (vert/rouge)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isOnline = value is bool b && b;
        return new SolidColorBrush(isOnline ? Color.FromRgb(34, 197, 94) : Color.FromRgb(239, 68, 68));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit un booléen en texte En ligne / Hors ligne
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "En ligne" : "Hors ligne";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Extrait la première lettre d'une chaîne
/// </summary>
public class FirstLetterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
        {
            return s[0].ToString().ToUpper();
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit une couleur hex en Brush
/// </summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit un nom d'icône en MaterialDesign PackIconKind
/// </summary>
public class IconNameToKindConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string iconName)
        {
            return iconName.ToLower() switch
            {
                "user" => MaterialDesignThemes.Wpf.PackIconKind.AccountTie,
                "usercheck" => MaterialDesignThemes.Wpf.PackIconKind.FaceWomanOutline,
                "baby" => MaterialDesignThemes.Wpf.PackIconKind.BabyFaceOutline,
                "droplets" => MaterialDesignThemes.Wpf.PackIconKind.ShowerHead,
                "man" => MaterialDesignThemes.Wpf.PackIconKind.HumanMale,
                "woman" => MaterialDesignThemes.Wpf.PackIconKind.HumanFemale,
                "child" => MaterialDesignThemes.Wpf.PackIconKind.HumanChild,
                "shower" => MaterialDesignThemes.Wpf.PackIconKind.ShowerHead,
                _ => MaterialDesignThemes.Wpf.PackIconKind.Account
            };
        }
        return MaterialDesignThemes.Wpf.PackIconKind.Account;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convertit un chemin d'image local en BitmapImage
/// </summary>
public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Retourne Visible si l'image locale existe, sinon Collapsed
/// </summary>
public class HasImageToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Retourne Visible si PAS d'image locale, sinon Collapsed (pour fallback icône)
/// </summary>
public class NoImageToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
