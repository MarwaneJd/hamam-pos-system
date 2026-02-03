using Microsoft.EntityFrameworkCore;
using HammamDesktop.Data.Entities;

namespace HammamDesktop.Data;

/// <summary>
/// DbContext SQLite pour le stockage local
/// </summary>
public class LocalDbContext : DbContext
{
    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
    {
    }

    public DbSet<LocalTicket> Tickets => Set<LocalTicket>();
    public DbSet<LocalTypeTicket> TypeTickets => Set<LocalTypeTicket>();
    public DbSet<LocalSession> Sessions => Set<LocalSession>();
    public DbSet<LocalConfig> Configs => Set<LocalConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Index sur les tickets pour les requêtes fréquentes
        modelBuilder.Entity<LocalTicket>(entity =>
        {
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.SyncStatus);
            entity.HasIndex(t => t.EmployeId);
            entity.HasIndex(t => new { t.EmployeId, t.CreatedAt });
        });

        // Seed des types de tickets par défaut
        modelBuilder.Entity<LocalTypeTicket>().HasData(
            new LocalTypeTicket
            {
                Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                Nom = "HOMME",
                Prix = 15.00m,
                Couleur = "#3B82F6",
                Ordre = 1
            },
            new LocalTypeTicket
            {
                Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"),
                Nom = "FEMME",
                Prix = 15.00m,
                Couleur = "#EC4899",
                Ordre = 2
            },
            new LocalTypeTicket
            {
                Id = Guid.Parse("aaaa3333-3333-3333-3333-333333333333"),
                Nom = "ENFANT",
                Prix = 10.00m,
                Couleur = "#10B981",
                Ordre = 3
            }
        );
    }
}
