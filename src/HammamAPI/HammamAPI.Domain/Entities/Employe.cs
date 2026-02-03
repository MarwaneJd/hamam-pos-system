using System;

namespace HammamAPI.Domain.Entities;

/// <summary>
/// Représente un employé travaillant dans un Hammam
/// </summary>
public class Employe
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordClair { get; set; } // Mot de passe en clair pour affichage admin
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public Guid HammamId { get; set; }
    public string Langue { get; set; } = "FR"; // FR ou AR
    public EmployeRole Role { get; set; } = EmployeRole.Employe;
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual Hammam Hammam { get; set; } = null!;
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public enum EmployeRole
{
    Employe = 0,
    Manager = 1,
    Admin = 2
}
