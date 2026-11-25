using System.Security.Cryptography;
using System.Text;

namespace HammamDesktop.Services;

/// <summary>
/// Chiffrement local du mot de passe via Windows DPAPI (lié au compte utilisateur Windows).
/// Permet une ré-authentification silencieuse au retour de la connexion,
/// sans jamais stocker le mot de passe en clair.
/// </summary>
public static class CryptoHelper
{
    public static string? Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain))
            return null;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Impossible de chiffrer le mot de passe");
            return null;
        }
    }

    public static string? Unprotect(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Impossible de déchiffrer le mot de passe en cache");
            return null;
        }
    }
}
