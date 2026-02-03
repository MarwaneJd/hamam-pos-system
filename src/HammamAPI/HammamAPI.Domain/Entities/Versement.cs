using System;

namespace HammamAPI.Domain.Entities;

/// <summary>
/// Représente un versement d'argent effectué par un employé à l'admin
/// Compare le montant théorique (ventes) vs montant réel remis
/// </summary>
public class Versement
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Employé qui fait le versement
    /// </summary>
    public Guid EmployeId { get; set; }
    
    /// <summary>
    /// Hammam concerné
    /// </summary>
    public Guid HammamId { get; set; }
    
    /// <summary>
    /// Date du versement (jour de travail)
    /// </summary>
    public DateTime DateVersement { get; set; }
    
    /// <summary>
    /// Montant théorique calculé (total des tickets vendus)
    /// </summary>
    public decimal MontantTheorique { get; set; }
    
    /// <summary>
    /// Montant réel remis par l'employé
    /// </summary>
    public decimal MontantRemis { get; set; }
    
    /// <summary>
    /// Écart = MontantRemis - MontantTheorique
    /// Positif = surplus, Négatif = déficit
    /// </summary>
    public decimal Ecart { get; set; }
    
    /// <summary>
    /// Nombre de tickets vendus ce jour
    /// </summary>
    public int NombreTickets { get; set; }
    
    /// <summary>
    /// Commentaire optionnel de l'admin
    /// </summary>
    public string? Commentaire { get; set; }
    
    /// <summary>
    /// Date de création de l'enregistrement
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Admin qui a validé le versement
    /// </summary>
    public Guid? ValidePar { get; set; }

    // Navigation properties
    public virtual Employe Employe { get; set; } = null!;
    public virtual Hammam Hammam { get; set; } = null!;
}
