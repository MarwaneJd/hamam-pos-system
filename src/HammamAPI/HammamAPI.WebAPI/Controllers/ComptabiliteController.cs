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
        [FromQuery] DateTime dateFin,
        [FromQuery] Guid? employeId = null)
    {
        var from = DateTime.SpecifyKind(dateDebut.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateFin.Date.AddDays(1), DateTimeKind.Utc);

        // Tickets de la période pour ce hammam (filtré par employé si spécifié)
        var ticketsQuery = _context.Tickets
            .Include(t => t.TypeTicket)
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= from && t.CreatedAt < to);

        if (employeId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.EmployeId == employeId.Value);

        var tickets = await ticketsQuery.ToListAsync();

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
            .OrderBy(d => d)
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
            for (var d = dateDebut.Date; d <= dateFin.Date; d = d.AddDays(1))
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
                TicketNumber = t.TicketNumber,
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

    /// <summary>
    /// Diagnostic : distribution des tickets par hammam + détection des mismatches
    /// (ticket.HammamId != employe.HammamId)
    /// </summary>
    [HttpGet("diagnostic")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DiagnosticResumeDto>> GetDiagnostic(
        [FromQuery] DateTime? dateDebut = null,
        [FromQuery] DateTime? dateFin = null)
    {
        var from = dateDebut.HasValue
            ? DateTime.SpecifyKind(dateDebut.Value.Date, DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-30), DateTimeKind.Utc);
        var to = dateFin.HasValue
            ? DateTime.SpecifyKind(dateFin.Value.Date.AddDays(1), DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);

        // Distribution des tickets par hammam
        var ticketsParHammam = await _context.Tickets
            .Include(t => t.Hammam)
            .Where(t => t.CreatedAt >= from && t.CreatedAt < to)
            .GroupBy(t => new { t.HammamId, t.Hammam.Nom })
            .Select(g => new HammamTicketCountDto
            {
                HammamId = g.Key.HammamId,
                HammamNom = g.Key.Nom,
                Count = g.Count(),
                Revenue = g.Sum(t => t.Prix)
            })
            .OrderByDescending(h => h.Count)
            .ToListAsync();

        // Tickets mal assignés : ticket.HammamId != employe.HammamId
        var mismatches = await _context.Tickets
            .Include(t => t.Employe)
            .ThenInclude(e => e.Hammam)
            .Include(t => t.Hammam)
            .Where(t => t.CreatedAt >= from && t.CreatedAt < to && t.HammamId != t.Employe.HammamId)
            .GroupBy(t => new
            {
                TicketHammamId = t.HammamId,
                TicketHammamNom = t.Hammam.Nom,
                EmployeHammamId = t.Employe.HammamId,
                EmployeHammamNom = t.Employe.Hammam.Nom
            })
            .Select(g => new MismatchGroupDto
            {
                TicketHammamId = g.Key.TicketHammamId,
                TicketHammamNom = g.Key.TicketHammamNom,
                EmployeHammamId = g.Key.EmployeHammamId,
                EmployeHammamNom = g.Key.EmployeHammamNom,
                Count = g.Count(),
                Revenue = g.Sum(t => t.Prix)
            })
            .OrderByDescending(m => m.Count)
            .ToListAsync();

        return new DiagnosticResumeDto
        {
            DateDebut = from,
            DateFin = dateFin?.Date ?? DateTime.UtcNow.Date,
            TotalTickets = ticketsParHammam.Sum(h => h.Count),
            TicketsParHammam = ticketsParHammam,
            Mismatches = mismatches,
            TotalMismatched = mismatches.Sum(m => m.Count)
        };
    }

    /// <summary>
    /// Preview : montre combien de tickets seraient réparés (dry-run)
    /// </summary>
    [HttpPost("repair-preview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RepairPreviewDto>> RepairPreview([FromBody] RepairRequest request)
    {
        var moves = await GetMismatchedTicketMoves(request);
        return new RepairPreviewDto
        {
            TotalAffected = moves.Sum(m => m.TicketCount),
            Moves = moves
        };
    }

    /// <summary>
    /// Exécute la réparation : réassigne ticket.HammamId = employe.HammamId
    /// </summary>
    [HttpPost("repair-execute")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RepairPreviewDto>> RepairExecute([FromBody] RepairRequest request)
    {
        var from = request.DateDebut.HasValue
            ? DateTime.SpecifyKind(request.DateDebut.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;
        var to = request.DateFin.HasValue
            ? DateTime.SpecifyKind(request.DateFin.Value.Date.AddDays(1), DateTimeKind.Utc)
            : (DateTime?)null;

        var query = _context.Tickets
            .Include(t => t.Employe)
            .Where(t => t.HammamId != t.Employe.HammamId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt < to.Value);
        if (request.SourceHammamId.HasValue)
            query = query.Where(t => t.HammamId == request.SourceHammamId.Value);

        var tickets = await query.ToListAsync();

        var repaired = 0;
        foreach (var ticket in tickets)
        {
            _logger.LogInformation(
                "Repair: Ticket {TicketId} HammamId {OldHammam} -> {NewHammam} (employe {EmpId})",
                ticket.Id, ticket.HammamId, ticket.Employe.HammamId, ticket.EmployeId);
            ticket.HammamId = ticket.Employe.HammamId;
            repaired++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Repair terminé : {Count} tickets corrigés", repaired);

        // Retourner le résumé des mouvements effectués
        var moves = tickets
            .GroupBy(t => new { From = t.HammamId, To = t.Employe.HammamId })
            .Select(g => new RepairMoveDto
            {
                FromHammamId = g.Key.From,
                ToHammamId = g.Key.To,
                TicketCount = g.Count(),
                Revenue = g.Sum(t => t.Prix)
            })
            .ToList();

        return new RepairPreviewDto
        {
            TotalAffected = repaired,
            Moves = moves
        };
    }

    private async Task<List<RepairMoveDto>> GetMismatchedTicketMoves(RepairRequest request)
    {
        var from = request.DateDebut.HasValue
            ? DateTime.SpecifyKind(request.DateDebut.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;
        var to = request.DateFin.HasValue
            ? DateTime.SpecifyKind(request.DateFin.Value.Date.AddDays(1), DateTimeKind.Utc)
            : (DateTime?)null;

        var query = _context.Tickets
            .Include(t => t.Employe).ThenInclude(e => e.Hammam)
            .Include(t => t.Hammam)
            .Where(t => t.HammamId != t.Employe.HammamId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt < to.Value);
        if (request.SourceHammamId.HasValue)
            query = query.Where(t => t.HammamId == request.SourceHammamId.Value);

        return await query
            .GroupBy(t => new
            {
                FromId = t.HammamId,
                FromNom = t.Hammam.Nom,
                ToId = t.Employe.HammamId,
                ToNom = t.Employe.Hammam.Nom
            })
            .Select(g => new RepairMoveDto
            {
                FromHammamId = g.Key.FromId,
                FromHammamNom = g.Key.FromNom,
                ToHammamId = g.Key.ToId,
                ToHammamNom = g.Key.ToNom,
                TicketCount = g.Count(),
                Revenue = g.Sum(t => t.Prix)
            })
            .OrderByDescending(m => m.TicketCount)
            .ToListAsync();
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
    public string TicketNumber { get; set; } = "";
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

// --- Diagnostic & Repair DTOs ---

public class DiagnosticResumeDto
{
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public int TotalTickets { get; set; }
    public List<HammamTicketCountDto> TicketsParHammam { get; set; } = new();
    public List<MismatchGroupDto> Mismatches { get; set; } = new();
    public int TotalMismatched { get; set; }
}

public class HammamTicketCountDto
{
    public Guid HammamId { get; set; }
    public string HammamNom { get; set; } = "";
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class MismatchGroupDto
{
    public Guid TicketHammamId { get; set; }
    public string TicketHammamNom { get; set; } = "";
    public Guid EmployeHammamId { get; set; }
    public string EmployeHammamNom { get; set; } = "";
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class RepairRequest
{
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public Guid? SourceHammamId { get; set; }
}

public class RepairPreviewDto
{
    public int TotalAffected { get; set; }
    public List<RepairMoveDto> Moves { get; set; } = new();
}

public class RepairMoveDto
{
    public Guid FromHammamId { get; set; }
    public string FromHammamNom { get; set; } = "";
    public Guid ToHammamId { get; set; }
    public string ToHammamNom { get; set; } = "";
    public int TicketCount { get; set; }
    public decimal Revenue { get; set; }
}

#endregion
