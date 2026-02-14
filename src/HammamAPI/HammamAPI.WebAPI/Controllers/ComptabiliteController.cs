using HammamAPI.Domain.Entities;
using HammamAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Contrôleur pour la comptabilité journalière par hammam
/// L'admin reçoit l'argent du jour complet, pas par employé
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
    /// Résumé journalier pour un hammam sur une période
    /// Chaque ligne = 1 jour avec total tickets, théorique, remis, écart
    /// </summary>
    [HttpGet("resume")]
    public async Task<ActionResult<ComptabiliteResumeDto>> GetResume(
        [FromQuery] Guid hammamId,
        [FromQuery] DateTime dateDebut,
        [FromQuery] DateTime dateFin)
    {
        var from = DateTime.SpecifyKind(dateDebut.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateFin.Date.AddDays(1), DateTimeKind.Utc);

        // Tickets de la période pour ce hammam (tous employés confondus)
        var tickets = await _context.Tickets
            .Include(t => t.TypeTicket)
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= from && t.CreatedAt < to)
            .ToListAsync();

        // Versements existants (par jour, sans filtre employé)
        var versements = await _context.Versements
            .Where(v => v.HammamId == hammamId && v.DateVersement >= from && v.DateVersement < to)
            .ToListAsync();

        // Grouper tickets par jour
        var ticketsParJour = tickets.GroupBy(t => t.CreatedAt.Date).ToDictionary(g => g.Key, g => g.ToList());

        // Collecter tous les jours (avec tickets ou avec versement)
        var tousLesJours = ticketsParJour.Keys
            .Union(versements.Select(v => v.DateVersement.Date))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var jours = new List<JourComptabiliteDto>();

        foreach (var jour in tousLesJours)
        {
            var ticketsJour = ticketsParJour.GetValueOrDefault(jour, new List<Ticket>());
            var montantTheorique = ticketsJour.Sum(t => t.Prix);
            var nombreTickets = ticketsJour.Count;

            // Versement du jour pour ce hammam (prendre le premier, normalement un seul par jour)
            var versement = versements.FirstOrDefault(v => v.DateVersement.Date == jour);

            // Détail par type de ticket
            var detailParType = ticketsJour
                .GroupBy(t => new { t.TypeTicket.Nom, t.TypeTicket.Couleur })
                .Select(g => new TypeTicketResumeDto
                {
                    Nom = g.Key.Nom,
                    Couleur = g.Key.Couleur,
                    Nombre = g.Count(),
                    Montant = g.Sum(t => t.Prix)
                })
                .OrderBy(t => t.Nom)
                .ToList();

            jours.Add(new JourComptabiliteDto
            {
                Date = jour,
                NombreTickets = nombreTickets,
                MontantTheorique = montantTheorique,
                MontantRemis = versement?.MontantRemis,
                Ecart = versement?.Ecart,
                VersementId = versement?.Id,
                Commentaire = versement?.Commentaire,
                EstValide = versement != null,
                DetailParType = detailParType
            });
        }

        // Si la période n'a aucun jour, ajouter les jours vides
        if (jours.Count == 0)
        {
            for (var d = dateFin.Date; d >= dateDebut.Date; d = d.AddDays(-1))
            {
                jours.Add(new JourComptabiliteDto
                {
                    Date = DateTime.SpecifyKind(d, DateTimeKind.Utc),
                    NombreTickets = 0,
                    MontantTheorique = 0,
                    DetailParType = new List<TypeTicketResumeDto>()
                });
            }
        }

        return new ComptabiliteResumeDto
        {
            HammamId = hammamId,
            DateDebut = from,
            DateFin = dateFin.Date,
            TotalTickets = tickets.Count,
            TotalTheorique = tickets.Sum(t => t.Prix),
            TotalRemis = versements.Sum(v => v.MontantRemis),
            TotalEcart = versements.Sum(v => v.Ecart),
            Jours = jours
        };
    }

    /// <summary>
    /// Détail d'un jour : liste de tous les tickets vendus
    /// </summary>
    [HttpGet("jour-detail")]
    public async Task<ActionResult<JourDetailCompletDto>> GetJourDetail(
        [FromQuery] Guid hammamId,
        [FromQuery] DateTime date)
    {
        var jour = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var jourFin = DateTime.SpecifyKind(date.Date.AddDays(1), DateTimeKind.Utc);

        var hammam = await _context.Hammams.FindAsync(hammamId);
        if (hammam == null)
            return NotFound("Hammam non trouvé");

        var tickets = await _context.Tickets
            .Include(t => t.TypeTicket)
            .Include(t => t.Employe)
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= jour && t.CreatedAt < jourFin)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var versement = await _context.Versements
            .FirstOrDefaultAsync(v => v.HammamId == hammamId && v.DateVersement >= jour && v.DateVersement < jourFin);

        var montantTheorique = tickets.Sum(t => t.Prix);

        return new JourDetailCompletDto
        {
            HammamId = hammamId,
            HammamNom = hammam.Nom,
            Date = jour,
            Tickets = tickets.Select(t => new TicketDetailDto
            {
                Id = t.Id,
                Heure = t.CreatedAt,
                TypeTicket = t.TypeTicket.Nom,
                Employe = $"{t.Employe.Prenom} {t.Employe.Nom}",
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
    /// Enregistrer/MAJ le versement du jour pour un hammam
    /// </summary>
    [HttpPost("versement")]
    public async Task<ActionResult<VersementResultDto>> SaveVersement([FromBody] SaveVersementDto dto)
    {
        var jour = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc);
        var jourFin = DateTime.SpecifyKind(dto.Date.Date.AddDays(1), DateTimeKind.Utc);

        var hammam = await _context.Hammams.FindAsync(dto.HammamId);
        if (hammam == null)
            return NotFound("Hammam non trouvé");

        // Total tickets du jour pour tout le hammam
        var tickets = await _context.Tickets
            .Where(t => t.HammamId == dto.HammamId && t.CreatedAt >= jour && t.CreatedAt < jourFin)
            .ToListAsync();

        var montantTheorique = tickets.Sum(t => t.Prix);
        var nombreTickets = tickets.Count;
        var ecart = dto.MontantRemis - montantTheorique;

        // Chercher un versement existant pour ce hammam + jour
        var versement = await _context.Versements
            .FirstOrDefaultAsync(v => v.HammamId == dto.HammamId && v.DateVersement >= jour && v.DateVersement < jourFin);

        if (versement == null)
        {
            versement = new Versement
            {
                Id = Guid.NewGuid(),
                EmployeId = null,
                HammamId = dto.HammamId,
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

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur sauvegarde versement Hammam={HammamId} Date={Date}", dto.HammamId, jour);
            return StatusCode(500, new { message = $"Erreur DB: {ex.InnerException?.Message ?? ex.Message}" });
        }

        _logger.LogInformation("Versement enregistré pour {Hammam} le {Date}: Théorique={Theorique}, Remis={Remis}, Écart={Ecart}",
            hammam.Nom, jour.ToShortDateString(), montantTheorique, dto.MontantRemis, ecart);

        return new VersementResultDto
        {
            VersementId = versement.Id,
            HammamNom = hammam.Nom,
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
    public int TotalTickets { get; set; }
    public decimal TotalTheorique { get; set; }
    public decimal TotalRemis { get; set; }
    public decimal TotalEcart { get; set; }
    public List<JourComptabiliteDto> Jours { get; set; } = new();
}

public class JourComptabiliteDto
{
    public DateTime Date { get; set; }
    public int NombreTickets { get; set; }
    public decimal MontantTheorique { get; set; }
    public decimal? MontantRemis { get; set; }
    public decimal? Ecart { get; set; }
    public Guid? VersementId { get; set; }
    public string? Commentaire { get; set; }
    public bool EstValide { get; set; }
    public List<TypeTicketResumeDto> DetailParType { get; set; } = new();
}

public class TypeTicketResumeDto
{
    public string Nom { get; set; } = "";
    public string Couleur { get; set; } = "";
    public int Nombre { get; set; }
    public decimal Montant { get; set; }
}

public class JourDetailCompletDto
{
    public Guid HammamId { get; set; }
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
    public string Employe { get; set; } = "";
    public decimal Prix { get; set; }
}

public class SaveVersementDto
{
    public Guid HammamId { get; set; }
    public DateTime Date { get; set; }
    public decimal MontantRemis { get; set; }
    public string? Commentaire { get; set; }
}

public class VersementResultDto
{
    public Guid VersementId { get; set; }
    public string HammamNom { get; set; } = "";
    public DateTime Date { get; set; }
    public int NombreTickets { get; set; }
    public decimal MontantTheorique { get; set; }
    public decimal MontantRemis { get; set; }
    public decimal Ecart { get; set; }
    public bool EstPositif { get; set; }
}

#endregion
