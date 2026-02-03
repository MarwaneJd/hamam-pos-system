using HammamAPI.Domain.Entities;
using HammamAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Contrôleur pour la gestion des versements et comptabilité employés
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComptabiliteController : ControllerBase
{
    private readonly HammamDbContext _context;
    private readonly ILogger<ComptabiliteController> _logger;

    public ComptabiliteController(HammamDbContext context, ILogger<ComptabiliteController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtenir le résumé des ventes par employé pour un hammam et une période
    /// </summary>
    [HttpGet("resume")]
    public async Task<ActionResult<ComptabiliteResumeDto>> GetResume(
        [FromQuery] Guid hammamId,
        [FromQuery] DateTime dateDebut,
        [FromQuery] DateTime dateFin)
    {
        // Convertir en UTC pour PostgreSQL
        var from = DateTime.SpecifyKind(dateDebut.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateFin.Date.AddDays(1), DateTimeKind.Utc);

        // Récupérer les employés du hammam
        var employes = await _context.Employes
            .Where(e => e.HammamId == hammamId)
            .ToListAsync();

        // Récupérer les tickets de la période
        var tickets = await _context.Tickets
            .Include(t => t.Employe)
            .Include(t => t.TypeTicket)
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= from && t.CreatedAt < to)
            .ToListAsync();

        // Récupérer les versements existants
        var versements = await _context.Versements
            .Where(v => v.HammamId == hammamId && v.DateVersement >= from && v.DateVersement < to)
            .ToListAsync();

        // Grouper par employé et par jour
        var resumeParEmploye = new List<EmployeComptabiliteDto>();

        foreach (var employe in employes)
        {
            var ticketsEmploye = tickets.Where(t => t.EmployeId == employe.Id).ToList();
            
            // Grouper par jour
            var joursDetails = new List<JourDetailDto>();
            var ticketsParJour = ticketsEmploye.GroupBy(t => t.CreatedAt.Date);

            foreach (var groupe in ticketsParJour)
            {
                var jour = groupe.Key;
                var montantTheorique = groupe.Sum(t => t.Prix);
                var nombreTickets = groupe.Count();

                // Chercher si un versement existe pour ce jour
                var versement = versements.FirstOrDefault(v => 
                    v.EmployeId == employe.Id && 
                    v.DateVersement.Date == jour);

                joursDetails.Add(new JourDetailDto
                {
                    Date = jour,
                    NombreTickets = nombreTickets,
                    MontantTheorique = montantTheorique,
                    MontantRemis = versement?.MontantRemis,
                    Ecart = versement?.Ecart,
                    VersementId = versement?.Id,
                    Commentaire = versement?.Commentaire,
                    EstValide = versement != null
                });
            }

            // Ajouter les jours sans tickets mais avec versement
            var joursAvecVersementSansTicket = versements
                .Where(v => v.EmployeId == employe.Id && 
                       !joursDetails.Any(j => j.Date.Date == v.DateVersement.Date))
                .ToList();

            foreach (var v in joursAvecVersementSansTicket)
            {
                joursDetails.Add(new JourDetailDto
                {
                    Date = v.DateVersement.Date,
                    NombreTickets = v.NombreTickets,
                    MontantTheorique = v.MontantTheorique,
                    MontantRemis = v.MontantRemis,
                    Ecart = v.Ecart,
                    VersementId = v.Id,
                    Commentaire = v.Commentaire,
                    EstValide = true
                });
            }

            var totalTheorique = joursDetails.Sum(j => j.MontantTheorique);
            var totalRemis = joursDetails.Where(j => j.MontantRemis.HasValue).Sum(j => j.MontantRemis!.Value);
            var totalEcart = joursDetails.Where(j => j.Ecart.HasValue).Sum(j => j.Ecart!.Value);

            resumeParEmploye.Add(new EmployeComptabiliteDto
            {
                EmployeId = employe.Id,
                EmployeNom = $"{employe.Prenom} {employe.Nom}",
                Username = employe.Username,
                TotalTickets = ticketsEmploye.Count,
                TotalTheorique = totalTheorique,
                TotalRemis = totalRemis,
                TotalEcart = totalEcart,
                JoursDetails = joursDetails.OrderByDescending(j => j.Date).ToList()
            });
        }

        return new ComptabiliteResumeDto
        {
            HammamId = hammamId,
            DateDebut = from,
            DateFin = dateFin.Date,
            Employes = resumeParEmploye.OrderBy(e => e.EmployeNom).ToList()
        };
    }

    /// <summary>
    /// Obtenir les détails d'un jour pour un employé
    /// </summary>
    [HttpGet("jour-detail")]
    public async Task<ActionResult<JourDetailCompletDto>> GetJourDetail(
        [FromQuery] Guid employeId,
        [FromQuery] DateTime date)
    {
        // Convertir en UTC pour PostgreSQL
        var jour = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var jourFin = DateTime.SpecifyKind(date.Date.AddDays(1), DateTimeKind.Utc);

        var employe = await _context.Employes
            .Include(e => e.Hammam)
            .FirstOrDefaultAsync(e => e.Id == employeId);

        if (employe == null)
            return NotFound("Employé non trouvé");

        // Tickets du jour
        var tickets = await _context.Tickets
            .Include(t => t.TypeTicket)
            .Where(t => t.EmployeId == employeId && t.CreatedAt >= jour && t.CreatedAt < jourFin)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        // Versement existant - utiliser comparaison de date UTC
        var versementDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var versement = await _context.Versements
            .FirstOrDefaultAsync(v => v.EmployeId == employeId && v.DateVersement >= versementDate && v.DateVersement < jourFin);

        var montantTheorique = tickets.Sum(t => t.Prix);

        return new JourDetailCompletDto
        {
            EmployeId = employeId,
            EmployeNom = $"{employe.Prenom} {employe.Nom}",
            HammamNom = employe.Hammam.Nom,
            Date = jour,
            Tickets = tickets.Select(t => new TicketDetailDto
            {
                Id = t.Id,
                Heure = t.CreatedAt,
                TypeTicket = t.TypeTicket.Nom,
                Prix = t.Prix
            }).ToList(),
            NombreTickets = tickets.Count,
            MontantTheorique = montantTheorique,
            MontantRemis = versement?.MontantRemis,
            Ecart = versement?.Ecart,
            Commentaire = versement?.Commentaire,
            VersementId = versement?.Id,
            EstValide = versement != null
        };
    }

    /// <summary>
    /// Enregistrer ou mettre à jour un versement
    /// </summary>
    [HttpPost("versement")]
    public async Task<ActionResult<VersementResultDto>> SaveVersement([FromBody] SaveVersementDto dto)
    {
        // Convertir en UTC pour PostgreSQL
        var jour = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);
        var jourFin = DateTime.SpecifyKind(dto.Date.Date.AddDays(1), DateTimeKind.Utc);

        // Vérifier l'employé
        var employe = await _context.Employes.FindAsync(dto.EmployeId);
        if (employe == null)
            return NotFound("Employé non trouvé");

        // Calculer le montant théorique
        var tickets = await _context.Tickets
            .Where(t => t.EmployeId == dto.EmployeId && t.CreatedAt >= jour && t.CreatedAt < jourFin)
            .ToListAsync();

        var montantTheorique = tickets.Sum(t => t.Prix);
        var nombreTickets = tickets.Count;
        var ecart = dto.MontantRemis - montantTheorique;

        // Chercher un versement existant
        var versement = await _context.Versements
            .FirstOrDefaultAsync(v => v.EmployeId == dto.EmployeId && v.DateVersement >= jour && v.DateVersement < jourFin);

        if (versement == null)
        {
            versement = new Versement
            {
                Id = Guid.NewGuid(),
                EmployeId = dto.EmployeId,
                HammamId = employe.HammamId,
                DateVersement = jour,
                CreatedAt = DateTime.UtcNow
            };
            _context.Versements.Add(versement);
        }

        versement.MontantTheorique = montantTheorique;
        versement.MontantRemis = dto.MontantRemis;
        versement.Ecart = ecart;
        versement.NombreTickets = nombreTickets;
        versement.Commentaire = dto.Commentaire;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Versement enregistré pour {Employe} le {Date}: Théorique={Theorique}, Remis={Remis}, Écart={Ecart}",
            $"{employe.Prenom} {employe.Nom}", jour.ToShortDateString(), montantTheorique, dto.MontantRemis, ecart);

        return new VersementResultDto
        {
            VersementId = versement.Id,
            EmployeNom = $"{employe.Prenom} {employe.Nom}",
            Date = jour,
            NombreTickets = nombreTickets,
            MontantTheorique = montantTheorique,
            MontantRemis = dto.MontantRemis,
            Ecart = ecart,
            EstPositif = ecart >= 0
        };
    }

    /// <summary>
    /// Supprimer un versement
    /// </summary>
    [HttpDelete("versement/{id}")]
    public async Task<IActionResult> DeleteVersement(Guid id)
    {
        var versement = await _context.Versements.FindAsync(id);
        if (versement == null)
            return NotFound();

        _context.Versements.Remove(versement);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

#region DTOs

public class ComptabiliteResumeDto
{
    public Guid HammamId { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public List<EmployeComptabiliteDto> Employes { get; set; } = new();
}

public class EmployeComptabiliteDto
{
    public Guid EmployeId { get; set; }
    public string EmployeNom { get; set; } = "";
    public string Username { get; set; } = "";
    public int TotalTickets { get; set; }
    public decimal TotalTheorique { get; set; }
    public decimal TotalRemis { get; set; }
    public decimal TotalEcart { get; set; }
    public List<JourDetailDto> JoursDetails { get; set; } = new();
}

public class JourDetailDto
{
    public DateTime Date { get; set; }
    public int NombreTickets { get; set; }
    public decimal MontantTheorique { get; set; }
    public decimal? MontantRemis { get; set; }
    public decimal? Ecart { get; set; }
    public Guid? VersementId { get; set; }
    public string? Commentaire { get; set; }
    public bool EstValide { get; set; }
}

public class JourDetailCompletDto
{
    public Guid EmployeId { get; set; }
    public string EmployeNom { get; set; } = "";
    public string HammamNom { get; set; } = "";
    public DateTime Date { get; set; }
    public List<TicketDetailDto> Tickets { get; set; } = new();
    public int NombreTickets { get; set; }
    public decimal MontantTheorique { get; set; }
    public decimal? MontantRemis { get; set; }
    public decimal? Ecart { get; set; }
    public string? Commentaire { get; set; }
    public Guid? VersementId { get; set; }
    public bool EstValide { get; set; }
}

public class TicketDetailDto
{
    public Guid Id { get; set; }
    public DateTime Heure { get; set; }
    public string TypeTicket { get; set; } = "";
    public decimal Prix { get; set; }
}

public class SaveVersementDto
{
    public Guid EmployeId { get; set; }
    public DateTime Date { get; set; }
    public decimal MontantRemis { get; set; }
    public string? Commentaire { get; set; }
}

public class VersementResultDto
{
    public Guid VersementId { get; set; }
    public string EmployeNom { get; set; } = "";
    public DateTime Date { get; set; }
    public int NombreTickets { get; set; }
    public decimal MontantTheorique { get; set; }
    public decimal MontantRemis { get; set; }
    public decimal Ecart { get; set; }
    public bool EstPositif { get; set; }
}

#endregion
