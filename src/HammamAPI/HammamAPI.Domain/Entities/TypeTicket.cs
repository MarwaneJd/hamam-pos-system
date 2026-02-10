using System;

namespace HammamAPI.Domain.Entities;

/// <summary>
/// Représente un type de ticket (HOMME, FEMME, ENFANT, DOUCHE, etc.)
/// Chaque Hammam peut avoir ses propres types/tarifs
/// </summary>
public class TypeTicket
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty; // HOMME, FEMME, ENFANT, DOUCHE
    public decimal Prix { get; set; }
    public string Couleur { get; set; } = "#3B82F6"; // Couleur pour l'affichage
    public string Icone { get; set; } = "User"; // Nom de l'icône (User, UserCheck, Baby, Droplets, etc.)
    public string? ImageUrl { get; set; } // URL de l'image/logo du produit
    public int Ordre { get; set; } = 0; // Ordre d'affichage
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Relation avec le Hammam (nullable pour les types globaux)
    public Guid? HammamId { get; set; }
    public virtual Hammam? Hammam { get; set; }

    // Navigation properties
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
