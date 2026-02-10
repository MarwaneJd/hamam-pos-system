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

    public DateTime? SyncedAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string SyncStatus { get; set; } = "Pending";

    [MaxLength(100)]
    public string? DeviceId { get; set; }

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
    public string? ImageUrl { get; set; } // URL de l'image sur le serveur

    [MaxLength(500)]
    public string? LocalImagePath { get; set; } // Chemin local de l'image téléchargée

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
