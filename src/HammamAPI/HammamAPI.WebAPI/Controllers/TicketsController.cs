using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Controller pour les opérations de synchronisation des tickets
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère un ticket par son ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDto>> GetById(Guid id)
    {
        var ticket = await _ticketService.GetByIdAsync(id);
        
        if (ticket == null)
            return NotFound();

        return Ok(ticket);
    }

    /// <summary>
    /// Récupère les tickets d'un hammam avec filtres de dates optionnels
    /// </summary>
    [HttpGet("hammam/{hammamId:guid}")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetByHammam(
        Guid hammamId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var tickets = await _ticketService.GetByHammamAsync(hammamId, from, to);
        return Ok(tickets);
    }

    /// <summary>
    /// Récupère les tickets d'un employé avec filtres de dates optionnels
    /// </summary>
    [HttpGet("employe/{employeId:guid}")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetByEmploye(
        Guid employeId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var tickets = await _ticketService.GetByEmployeAsync(employeId, from, to);
        return Ok(tickets);
    }

    /// <summary>
    /// Crée un nouveau ticket (utilisé par l'app desktop quand en ligne)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create([FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = await _ticketService.CreateAsync(request);
            _logger.LogInformation("Ticket créé: {TicketId} par employé {EmployeId}", 
                ticket.Id, ticket.EmployeId);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du ticket");
            return StatusCode(500, new { message = "Erreur lors de la création du ticket" });
        }
    }

    /// <summary>
    /// Synchronisation massive de tickets depuis l'application desktop
    /// Endpoint principal pour la synchronisation offline/online
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncTicketsResponse>> SyncTickets([FromBody] SyncTicketsRequest request)
    {
        try
        {
            _logger.LogInformation("Début synchronisation de {Count} tickets", request.Tickets.Count());
            
            var result = await _ticketService.SyncTicketsAsync(request);
            
            _logger.LogInformation(
                "Synchronisation terminée: {Inserted} insérés, {Updated} mis à jour, {Errors} erreurs",
                result.Inserted, result.Updated, result.Errors);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la synchronisation des tickets");
            return StatusCode(500, new { message = "Erreur lors de la synchronisation" });
        }
    }

    /// <summary>
    /// Récupère le nombre de tickets vendus aujourd'hui
    /// </summary>
    [HttpGet("count/today")]
    public async Task<ActionResult<int>> GetTodayCount(
        [FromQuery] Guid? hammamId = null,
        [FromQuery] Guid? employeId = null)
    {
        var count = await _ticketService.GetTodayCountAsync(hammamId, employeId);
        return Ok(count);
    }

    /// <summary>
    /// Récupère le revenu total d'aujourd'hui
    /// </summary>
    [HttpGet("revenue/today")]
    public async Task<ActionResult<decimal>> GetTodayRevenue(
        [FromQuery] Guid? hammamId = null,
        [FromQuery] Guid? employeId = null)
    {
        var revenue = await _ticketService.GetTodayRevenueAsync(hammamId, employeId);
        return Ok(revenue);
    }
}
