using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HammamDesktop.Data.Entities;

/// <summary>
/// Ticket stocké localement dans SQLite
/// </summary>
[Table("tickets")]
public class LocalTicket
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TypeTicketId { get; set; }

    [Required]
    public Guid EmployeId { get; set; }

    [Required]
    public Guid HammamId { get; set; }

    [Required]
    public decimal Prix { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [MaxLength(50)]
    public string? TicketNumber { get; set; }

    public DateTime? SyncedAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string SyncStatus { get; set; } = "Pending";

    [MaxLength(100)]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Nombre de refus du serveur. Les tickets refusés passent en fin de file
    /// pour ne jamais bloquer la synchronisation des autres.
    /// </summary>
    public int SyncAttempts { get; set; }

    [MaxLength(500)]
    public string? LastSyncError { get; set; }

    // Informations de type (pour affichage offline)
    [MaxLength(50)]
    public string TypeTicketNom { get; set; } = string.Empty;
}

/// <summary>
/// Type de ticket en cache local
/// </summary>
[Table("type_tickets")]
public class LocalTypeTicket
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nom { get; set; } = string.Empty;

    public decimal Prix { get; set; }

    [MaxLength(20)]
    public string Couleur { get; set; } = "#3B82F6";

    [MaxLength(50)]
    public string Icone { get; set; } = "User"; // Nom de l'icône (User, UserCheck, Baby, Droplets)

    [MaxLength(500)]
    public string? ImageUrl { get; set; } // URL de l'image (serveur)

    [MaxLength(500)]
    public string? LocalImagePath { get; set; } // Chemin local de l'image cachée

    public int Ordre { get; set; }
}

/// <summary>
/// Session employé stockée localement
/// </summary>
[Table("sessions")]
public class LocalSession
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EmployeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Prenom { get; set; } = string.Empty;

    [Required]
    public Guid HammamId { get; set; }

    [MaxLength(100)]
    public string HammamNom { get; set; } = string.Empty;

    [MaxLength(100)]
    public string HammamNomArabe { get; set; } = string.Empty;

    public int HammamPrefixeTicket { get; set; } = 100000;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Profil employé en cache local (pour connexion hors ligne)
/// </summary>
[Table("employe_profiles")]
public class LocalEmployeProfile
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = "";

    [MaxLength(100)]
    public string Prenom { get; set; } = "";

    [MaxLength(100)]
    public string Nom { get; set; } = "";

    [MaxLength(20)]
    public string Icone { get; set; } = "User1";

    [Required]
    public Guid HammamId { get; set; }

    [MaxLength(100)]
    public string HammamNom { get; set; } = "";

    [MaxLength(100)]
    public string HammamNomArabe { get; set; } = "";

    public int HammamPrefixeTicket { get; set; } = 100000;

    [MaxLength(128)]
    public string PasswordHash { get; set; } = "";

    /// <summary>
    /// Mot de passe chiffré localement (DPAPI) pour ré-authentification silencieuse
    /// au retour de la connexion. Jamais stocké en clair.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Hash BCrypt fourni par le serveur (api/auth/offline-credentials) : permet à l'employé
    /// de se connecter hors ligne sur ce poste même s'il ne s'y est jamais connecté en ligne.
    /// </summary>
    public string? ServerPasswordHash { get; set; }

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configuration locale
/// </summary>
[Table("config")]
public class LocalConfig
{
    [Key]
    [MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
