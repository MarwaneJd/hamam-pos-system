using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Controller pour les statistiques du dashboard admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;
    private readonly ILogger<StatsController> _logger;

    public StatsController(IStatsService statsService, ILogger<StatsController> logger)
    {
        _statsService = statsService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère toutes les statistiques pour le dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var stats = await _statsService.GetDashboardStatsAsync(from, to);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des stats du dashboard");
            return StatusCode(500, new { message = "Erreur interne" });
        }
    }

    /// <summary>
    /// Statistiques par hammam
    /// </summary>
    [HttpGet("hammams")]
    public async Task<ActionResult<IEnumerable<HammamStatsDto>>> GetHammamStats(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        try
        {
            var stats = await _statsService.GetHammamStatsAsync(from, to);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des stats par hammam");
            return StatusCode(500, new { message = "Erreur interne" });
        }
    }

    /// <summary>
    /// Statistiques par employé avec classement
    /// </summary>
    [HttpGet("employes")]
    public async Task<ActionResult<IEnumerable<EmployeStatsDto>>> GetEmployeStats(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        try
        {
            var stats = await _statsService.GetEmployeStatsAsync(from, to);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des stats par employé");
            return StatusCode(500, new { message = "Erreur interne" });
        }
    }

    /// <summary>
    /// Calcule l'écart de caisse pour un hammam
    /// </summary>
    [HttpGet("ecart/{hammamId:guid}")]
    public async Task<ActionResult<decimal>> GetEcart(Guid hammamId, [FromQuery] DateTime date)
    {
        try
        {
            var ecart = await _statsService.CalculerEcartAsync(hammamId, date);
            return Ok(new { ecart, hasAlert = Math.Abs(ecart) > 0.05m * await GetExpectedRevenue(hammamId, date) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du calcul de l'écart");
            return StatusCode(500, new { message = "Erreur interne" });
        }
    }

    private Task<decimal> GetExpectedRevenue(Guid hammamId, DateTime date)
    {
        // Simplifié - en réalité, calculer le revenu attendu
        return Task.FromResult(1000m);
    }
}
