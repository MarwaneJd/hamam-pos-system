using System;

namespace HammamAPI.Domain.Entities;

/// <summary>
/// Représente un établissement Hammam
/// </summary>
public class Hammam
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Employe> Employes { get; set; } = new List<Employe>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<TypeTicket> TypeTickets { get; set; } = new List<TypeTicket>();
}
