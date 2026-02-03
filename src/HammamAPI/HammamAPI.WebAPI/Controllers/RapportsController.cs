using HammamAPI.Application.DTOs;
using HammamAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Controller pour la génération de rapports
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class RapportsController : ControllerBase
{
    private readonly HammamDbContext _context;
    private readonly ILogger<RapportsController> _logger;

    public RapportsController(HammamDbContext context, ILogger<RapportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Prévisualiser les données d'un rapport
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<RapportPreviewDto>> Preview([FromBody] RapportRequest request)
    {
        try
        {
            var query = _context.Tickets
                .Include(t => t.Employe)
                .Include(t => t.Hammam)
                .Include(t => t.TypeTicket)
                .Where(t => t.CreatedAt >= request.From && t.CreatedAt <= request.To);

            // Filtrer par hammams si spécifié
            if (request.HammamIds != null && request.HammamIds.Any())
            {
                query = query.Where(t => request.HammamIds.Contains(t.HammamId));
            }

            // Filtrer par employés si spécifié
            if (request.EmployeIds != null && request.EmployeIds.Any())
            {
                query = query.Where(t => request.EmployeIds.Contains(t.EmployeId));
            }

            var tickets = await query.ToListAsync();

            var totalTickets = tickets.Count;
            var totalRevenue = tickets.Sum(t => t.Prix);

            // Stats par hammam
            var lignesParHammam = tickets
                .GroupBy(t => t.Hammam?.Nom ?? "Inconnu")
                .Select(g => new RapportLigneDto(
                    g.Key,
                    g.Count(),
                    g.Sum(t => t.Prix),
                    totalRevenue > 0 ? Math.Round(g.Sum(t => t.Prix) / totalRevenue * 100, 2) : 0
                ))
                .OrderByDescending(l => l.Revenue)
                .ToList();

            // Stats par employé
            var lignesParEmploye = tickets
                .GroupBy(t => $"{t.Employe?.Prenom} {t.Employe?.Nom}".Trim())
                .Select(g => new RapportLigneDto(
                    string.IsNullOrEmpty(g.Key) ? "Inconnu" : g.Key,
                    g.Count(),
                    g.Sum(t => t.Prix),
                    totalRevenue > 0 ? Math.Round(g.Sum(t => t.Prix) / totalRevenue * 100, 2) : 0
                ))
                .OrderByDescending(l => l.Revenue)
                .ToList();

            // Stats par type de ticket
            var lignesParType = tickets
                .GroupBy(t => t.TypeTicket?.Nom ?? "Inconnu")
                .Select(g => new RapportLigneDto(
                    g.Key,
                    g.Count(),
                    g.Sum(t => t.Prix),
                    totalTickets > 0 ? Math.Round((decimal)g.Count() / totalTickets * 100, 2) : 0
                ))
                .OrderByDescending(l => l.TicketsCount)
                .ToList();

            // Stats par jour
            var lignesParJour = tickets
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new RapportJournalierDto(
                    g.Key,
                    g.Count(),
                    g.Sum(t => t.Prix)
                ))
                .OrderBy(l => l.Date)
                .ToList();

            return Ok(new RapportPreviewDto(
                totalTickets,
                totalRevenue,
                request.From,
                request.To,
                lignesParHammam,
                lignesParEmploye,
                lignesParType,
                lignesParJour
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération du rapport");
            return StatusCode(500, new { message = "Erreur lors de la génération du rapport" });
        }
    }

    /// <summary>
    /// Télécharger le rapport en Excel (format CSV compatible Excel)
    /// </summary>
    [HttpPost("excel")]
    public async Task<IActionResult> DownloadExcel([FromBody] RapportRequest request)
    {
        var preview = await GeneratePreviewData(request);
        
        // Générer CSV avec séparateur point-virgule pour Excel français
        var csv = GenerateCsvForExcel(preview, request);
        
        // BOM UTF-8 pour que Excel reconnaisse l'encodage
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var result = new byte[bom.Length + csvBytes.Length];
        bom.CopyTo(result, 0);
        csvBytes.CopyTo(result, bom.Length);
        
        return File(
            result,
            "text/csv; charset=utf-8",
            $"Rapport_{request.From:yyyy-MM-dd}.csv"
        );
    }

    /// <summary>
    /// Télécharger le rapport en CSV
    /// </summary>
    [HttpPost("csv")]
    public async Task<IActionResult> DownloadCsv([FromBody] RapportRequest request)
    {
        var preview = await GeneratePreviewData(request);
        var csv = GenerateCsv(preview, request);
        
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"Rapport_Hammam_{request.From:yyyy-MM-dd}_au_{request.To:yyyy-MM-dd}.csv"
        );
    }

    /// <summary>
    /// Télécharger le rapport en format texte (rapport formaté)
    /// </summary>
    [HttpPost("pdf")]
    public async Task<IActionResult> DownloadPdf([FromBody] RapportRequest request)
    {
        var preview = await GeneratePreviewData(request);
        
        // Générer un rapport texte bien formaté
        var content = GenerateTextReport(preview, request);
        
        // BOM UTF-8 pour encodage correct
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var result = new byte[bom.Length + contentBytes.Length];
        bom.CopyTo(result, 0);
        contentBytes.CopyTo(result, bom.Length);
        
        return File(
            result,
            "text/plain; charset=utf-8",
            $"Rapport_{request.From:yyyy-MM-dd}.txt"
        );
    }

    private async Task<RapportPreviewDto> GeneratePreviewData(RapportRequest request)
    {
        var query = _context.Tickets
            .Include(t => t.Employe)
            .Include(t => t.Hammam)
            .Include(t => t.TypeTicket)
            .Where(t => t.CreatedAt >= request.From && t.CreatedAt <= request.To);

        if (request.HammamIds != null && request.HammamIds.Any())
        {
            query = query.Where(t => request.HammamIds.Contains(t.HammamId));
        }

        if (request.EmployeIds != null && request.EmployeIds.Any())
        {
            query = query.Where(t => request.EmployeIds.Contains(t.EmployeId));
        }

        var tickets = await query.ToListAsync();

        var totalTickets = tickets.Count;
        var totalRevenue = tickets.Sum(t => t.Prix);

        var lignesParHammam = tickets
            .GroupBy(t => t.Hammam?.Nom ?? "Inconnu")
            .Select(g => new RapportLigneDto(g.Key, g.Count(), g.Sum(t => t.Prix), totalRevenue > 0 ? Math.Round(g.Sum(t => t.Prix) / totalRevenue * 100, 2) : 0))
            .OrderByDescending(l => l.Revenue)
            .ToList();

        var lignesParEmploye = tickets
            .GroupBy(t => $"{t.Employe?.Prenom} {t.Employe?.Nom}".Trim())
            .Select(g => new RapportLigneDto(string.IsNullOrEmpty(g.Key) ? "Inconnu" : g.Key, g.Count(), g.Sum(t => t.Prix), totalRevenue > 0 ? Math.Round(g.Sum(t => t.Prix) / totalRevenue * 100, 2) : 0))
            .OrderByDescending(l => l.Revenue)
            .ToList();

        var lignesParType = tickets
            .GroupBy(t => t.TypeTicket?.Nom ?? "Inconnu")
            .Select(g => new RapportLigneDto(g.Key, g.Count(), g.Sum(t => t.Prix), totalTickets > 0 ? Math.Round((decimal)g.Count() / totalTickets * 100, 2) : 0))
            .OrderByDescending(l => l.TicketsCount)
            .ToList();

        var lignesParJour = tickets
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new RapportJournalierDto(g.Key, g.Count(), g.Sum(t => t.Prix)))
            .OrderBy(l => l.Date)
            .ToList();

        return new RapportPreviewDto(totalTickets, totalRevenue, request.From, request.To, lignesParHammam, lignesParEmploye, lignesParType, lignesParJour);
    }

    private string GenerateCsvForExcel(RapportPreviewDto preview, RapportRequest request)
    {
        var lines = new List<string>
        {
            "RAPPORT HAMMAM",
            $"Période;Du {request.From:dd/MM/yyyy} au {request.To:dd/MM/yyyy}",
            $"Total Tickets;{preview.TotalTickets}",
            $"Total Revenus;{preview.TotalRevenue} DH",
            "",
            "PAR HAMMAM",
            "Hammam;Tickets;Revenus (DH);Pourcentage"
        };

        foreach (var ligne in preview.LignesParHammam)
        {
            lines.Add($"{ligne.Label};{ligne.TicketsCount};{ligne.Revenue};{ligne.Pourcentage}%");
        }

        lines.Add("");
        lines.Add("PAR EMPLOYE");
        lines.Add("Employé;Tickets;Revenus (DH);Pourcentage");

        foreach (var ligne in preview.LignesParEmploye)
        {
            lines.Add($"{ligne.Label};{ligne.TicketsCount};{ligne.Revenue};{ligne.Pourcentage}%");
        }

        lines.Add("");
        lines.Add("PAR TYPE");
        lines.Add("Type;Tickets;Revenus (DH);Pourcentage");

        foreach (var ligne in preview.LignesParType)
        {
            lines.Add($"{ligne.Label};{ligne.TicketsCount};{ligne.Revenue};{ligne.Pourcentage}%");
        }

        lines.Add("");
        lines.Add("PAR JOUR");
        lines.Add("Date;Tickets;Revenus (DH)");

        foreach (var ligne in preview.LignesParJour)
        {
            lines.Add($"{ligne.Date:dd/MM/yyyy};{ligne.TicketsCount};{ligne.Revenue}");
        }

        return string.Join("\r\n", lines);
    }

    private string GenerateCsv(RapportPreviewDto preview, RapportRequest request)
    {
        var lines = new List<string>
        {
            "RAPPORT HAMMAM",
            $"Période: Du {request.From:dd/MM/yyyy} au {request.To:dd/MM/yyyy}",
            $"Total Tickets: {preview.TotalTickets}",
            $"Total Revenus: {preview.TotalRevenue} DH",
            "",
            "=== PAR HAMMAM ===",
            "Hammam;Tickets;Revenus (DH);Pourcentage"
        };

        foreach (var ligne in preview.LignesParHammam)
        {
            lines.Add($"{ligne.Label};{ligne.TicketsCount};{ligne.Revenue};{ligne.Pourcentage}%");
        }

        lines.Add("");
        lines.Add("=== PAR EMPLOYE ===");
        lines.Add("Employé;Tickets;Revenus (DH);Pourcentage");

        foreach (var ligne in preview.LignesParEmploye)
        {
            lines.Add($"{ligne.Label};{ligne.TicketsCount};{ligne.Revenue};{ligne.Pourcentage}%");
        }

        lines.Add("");
        lines.Add("=== PAR TYPE ===");
        lines.Add("Type;Tickets;Revenus (DH);Pourcentage");

        foreach (var ligne in preview.LignesParType)
        {
            lines.Add($"{ligne.Label};{ligne.TicketsCount};{ligne.Revenue};{ligne.Pourcentage}%");
        }

        lines.Add("");
        lines.Add("=== PAR JOUR ===");
        lines.Add("Date;Tickets;Revenus (DH)");

        foreach (var ligne in preview.LignesParJour)
        {
            lines.Add($"{ligne.Date:dd/MM/yyyy};{ligne.TicketsCount};{ligne.Revenue}");
        }

        return string.Join("\n", lines);
    }

    private string GenerateTextReport(RapportPreviewDto preview, RapportRequest request)
    {
        var lines = new List<string>
        {
            "╔══════════════════════════════════════════════════════════╗",
            "║                    RAPPORT HAMMAM                        ║",
            "╚══════════════════════════════════════════════════════════╝",
            "",
            $"  Période: Du {request.From:dd/MM/yyyy} au {request.To:dd/MM/yyyy}",
            $"  Total Tickets: {preview.TotalTickets}",
            $"  Total Revenus: {preview.TotalRevenue} DH",
            "",
            "──────────────────────────────────────────────────────────",
            "  STATISTIQUES PAR HAMMAM",
            "──────────────────────────────────────────────────────────"
        };

        foreach (var ligne in preview.LignesParHammam)
        {
            lines.Add($"  {ligne.Label,-25} {ligne.TicketsCount,5} tickets    {ligne.Revenue,8} DH ({ligne.Pourcentage}%)");
        }

        lines.Add("");
        lines.Add("──────────────────────────────────────────────────────────");
        lines.Add("  STATISTIQUES PAR EMPLOYE");
        lines.Add("──────────────────────────────────────────────────────────");

        foreach (var ligne in preview.LignesParEmploye.Take(10))
        {
            lines.Add($"  {ligne.Label,-25} {ligne.TicketsCount,5} tickets    {ligne.Revenue,8} DH");
        }

        lines.Add("");
        lines.Add("──────────────────────────────────────────────────────────");
        lines.Add("  STATISTIQUES PAR TYPE DE TICKET");
        lines.Add("──────────────────────────────────────────────────────────");

        foreach (var ligne in preview.LignesParType)
        {
            lines.Add($"  {ligne.Label,-15} {ligne.TicketsCount,5} tickets    {ligne.Revenue,8} DH ({ligne.Pourcentage}%)");
        }

        lines.Add("");
        lines.Add("══════════════════════════════════════════════════════════");
        lines.Add($"  Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}");
        lines.Add("══════════════════════════════════════════════════════════");

        return string.Join("\n", lines);
    }
}
