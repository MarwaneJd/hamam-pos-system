using System;

namespace HammamAPI.Domain.Entities;

/// <summary>
/// Représente un ticket de vente
/// L'ID est un UUID généré localement par l'application desktop
/// </summary>
public class Ticket
{
    public Guid Id { get; set; } // UUID généré localement
    public Guid TypeTicketId { get; set; }
    public Guid EmployeId { get; set; }
    public Guid HammamId { get; set; }
    public decimal Prix { get; set; } // Prix au moment de la vente
    public DateTime CreatedAt { get; set; } // Date/heure de la vente
    public DateTime? SyncedAt { get; set; } // Date de synchronisation (null si non sync)
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? DeviceId { get; set; } // Identifiant du PC qui a créé le ticket

    // Navigation properties
    public virtual TypeTicket TypeTicket { get; set; } = null!;
    public virtual Employe Employe { get; set; } = null!;
    public virtual Hammam Hammam { get; set; } = null!;
}

public enum SyncStatus
{
    Pending = 0,    // En attente de synchronisation
    Synced = 1,     // Synchronisé avec succès
    Error = 2,      // Erreur de synchronisation
    Conflict = 3    // Conflit détecté
}
