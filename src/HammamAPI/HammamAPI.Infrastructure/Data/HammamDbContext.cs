using HammamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.Infrastructure.Data;

/// <summary>
/// DbContext principal pour PostgreSQL
/// </summary>
public class HammamDbContext : DbContext
{
    public HammamDbContext(DbContextOptions<HammamDbContext> options) : base(options)
    {
    }

    public DbSet<Hammam> Hammams => Set<Hammam>();
    public DbSet<Employe> Employes => Set<Employe>();
    public DbSet<TypeTicket> TypeTickets => Set<TypeTicket>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Versement> Versements => Set<Versement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration Hammam
        modelBuilder.Entity<Hammam>(entity =>
        {
            entity.ToTable("hammam");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            entity.Property(h => h.Nom).HasColumnName("nom").HasMaxLength(100).IsRequired();
            entity.Property(h => h.Adresse).HasColumnName("adresse").HasMaxLength(255);
            entity.Property(h => h.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(h => h.CreatedAt).HasColumnName("created_at");
            entity.Property(h => h.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(h => h.Code).IsUnique();

            // Relations
            entity.HasMany(h => h.Employes)
                  .WithOne(e => e.Hammam)
                  .HasForeignKey(e => e.HammamId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(h => h.Tickets)
                  .WithOne(t => t.Hammam)
                  .HasForeignKey(t => t.HammamId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(h => h.TypeTickets)
                  .WithOne(t => t.Hammam)
                  .HasForeignKey(t => t.HammamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration Employe
        modelBuilder.Entity<Employe>(entity =>
        {
            entity.ToTable("employe");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Nom).HasColumnName("nom").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Prenom).HasColumnName("prenom").HasMaxLength(100).IsRequired();
            entity.Property(e => e.HammamId).HasColumnName("hammam_id");
            entity.Property(e => e.Langue).HasColumnName("langue").HasMaxLength(2).HasDefaultValue("FR");
            entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");

            // Username n'est plus unique - plusieurs employés peuvent avoir le même username (Utilisateur1, Utilisateur2)
            // dans différents hammams, différenciés par leur mot de passe
            entity.HasIndex(e => e.Username);

            // Relations
            entity.HasMany(e => e.Tickets)
                  .WithOne(t => t.Employe)
                  .HasForeignKey(t => t.EmployeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration TypeTicket
        modelBuilder.Entity<TypeTicket>(entity =>
        {
            entity.ToTable("type_ticket");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.Nom).HasColumnName("nom").HasMaxLength(50).IsRequired();
            entity.Property(t => t.Prix).HasColumnName("prix").HasPrecision(10, 2);
            entity.Property(t => t.Couleur).HasColumnName("couleur").HasMaxLength(20);
            entity.Property(t => t.Icone).HasColumnName("icone").HasMaxLength(50).HasDefaultValue("User");
            entity.Property(t => t.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            entity.Property(t => t.Ordre).HasColumnName("ordre");
            entity.Property(t => t.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.HammamId).HasColumnName("hammam_id");

            // Relations
            entity.HasMany(tt => tt.Tickets)
                  .WithOne(t => t.TypeTicket)
                  .HasForeignKey(t => t.TypeTicketId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration Ticket
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("ticket");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.TypeTicketId).HasColumnName("type_ticket_id");
            entity.Property(t => t.EmployeId).HasColumnName("employe_id");
            entity.Property(t => t.HammamId).HasColumnName("hammam_id");
            entity.Property(t => t.Prix).HasColumnName("prix").HasPrecision(10, 2);
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.SyncedAt).HasColumnName("synced_at");
            entity.Property(t => t.SyncStatus).HasColumnName("sync_status").HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.DeviceId).HasColumnName("device_id").HasMaxLength(100);

            // Index pour les requêtes fréquentes
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.HammamId);
            entity.HasIndex(t => t.EmployeId);
            entity.HasIndex(t => new { t.HammamId, t.CreatedAt });
            entity.HasIndex(t => new { t.EmployeId, t.CreatedAt });
        });

        // Configuration Versement
        modelBuilder.Entity<Versement>(entity =>
        {
            entity.ToTable("versement");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasColumnName("id");
            entity.Property(v => v.EmployeId).HasColumnName("employe_id");
            entity.Property(v => v.HammamId).HasColumnName("hammam_id");
            entity.Property(v => v.DateVersement).HasColumnName("date_versement");
            entity.Property(v => v.MontantTheorique).HasColumnName("montant_theorique").HasPrecision(10, 2);
            entity.Property(v => v.MontantRemis).HasColumnName("montant_remis").HasPrecision(10, 2);
            entity.Property(v => v.Ecart).HasColumnName("ecart").HasPrecision(10, 2);
            entity.Property(v => v.NombreTickets).HasColumnName("nombre_tickets");
            entity.Property(v => v.Commentaire).HasColumnName("commentaire").HasMaxLength(500);
            entity.Property(v => v.CreatedAt).HasColumnName("created_at");
            entity.Property(v => v.ValidePar).HasColumnName("valide_par");

            // Index pour les requêtes
            entity.HasIndex(v => v.DateVersement);
            entity.HasIndex(v => v.EmployeId);
            entity.HasIndex(v => v.HammamId);
            entity.HasIndex(v => new { v.EmployeId, v.DateVersement }).IsUnique();

            // Relations
            entity.HasOne(v => v.Employe)
                  .WithMany()
                  .HasForeignKey(v => v.EmployeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Hammam)
                  .WithMany()
                  .HasForeignKey(v => v.HammamId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed des données initiales
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed des 6 Hammams
        var hammams = new[]
        {
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "HAM001", Nom = "Hammam Centre", Adresse = "123 Rue Principale, Casablanca", Actif = true, CreatedAt = DateTime.UtcNow },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "HAM002", Nom = "Hammam Anfa", Adresse = "45 Boulevard Anfa, Casablanca", Actif = true, CreatedAt = DateTime.UtcNow },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Code = "HAM003", Nom = "Hammam Maarif", Adresse = "78 Rue Maarif, Casablanca", Actif = true, CreatedAt = DateTime.UtcNow },
            new { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Code = "HAM004", Nom = "Hammam Hay Mohammadi", Adresse = "12 Avenue Hassan II, Casablanca", Actif = true, CreatedAt = DateTime.UtcNow },
            new { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Code = "HAM005", Nom = "Hammam Derb Sultan", Adresse = "90 Derb Sultan, Casablanca", Actif = true, CreatedAt = DateTime.UtcNow },
            new { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Code = "HAM006", Nom = "Hammam Sidi Moumen", Adresse = "34 Quartier Sidi Moumen, Casablanca", Actif = false, CreatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<Hammam>().HasData(
            hammams.Select(h => new Hammam
            {
                Id = h.Id,
                Code = h.Code,
                Nom = h.Nom,
                Adresse = h.Adresse,
                Actif = h.Actif,
                CreatedAt = h.CreatedAt
            })
        );

        // Seed des Types de Tickets avec icônes
        modelBuilder.Entity<TypeTicket>().HasData(
            new TypeTicket { Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"), Nom = "HOMME", Prix = 15.00m, Couleur = "#3B82F6", Icone = "User", Ordre = 1, Actif = true, CreatedAt = DateTime.UtcNow },
            new TypeTicket { Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"), Nom = "FEMME", Prix = 15.00m, Couleur = "#EC4899", Icone = "UserCheck", Ordre = 2, Actif = true, CreatedAt = DateTime.UtcNow },
            new TypeTicket { Id = Guid.Parse("aaaa3333-3333-3333-3333-333333333333"), Nom = "ENFANT", Prix = 10.00m, Couleur = "#10B981", Icone = "Baby", Ordre = 3, Actif = true, CreatedAt = DateTime.UtcNow },
            new TypeTicket { Id = Guid.Parse("aaaa4444-4444-4444-4444-444444444444"), Nom = "DOUCHE", Prix = 8.00m, Couleur = "#06B6D4", Icone = "Droplets", Ordre = 4, Actif = true, CreatedAt = DateTime.UtcNow }
        );

        // Seed d'un admin par défaut (mot de passe: Admin@123)
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        modelBuilder.Entity<Employe>().HasData(
            new Employe
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "admin",
                PasswordHash = adminPasswordHash,
                Nom = "Administrateur",
                Prenom = "System",
                HammamId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Langue = "FR",
                Role = EmployeRole.Admin,
                Actif = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
