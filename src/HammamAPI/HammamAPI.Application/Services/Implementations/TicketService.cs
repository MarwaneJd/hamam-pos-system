using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using HammamAPI.Domain.Entities;
using HammamAPI.Domain.Interfaces;

namespace HammamAPI.Application.Services.Implementations;

/// <summary>
/// Service de gestion des tickets avec synchronisation
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITypeTicketRepository _typeTicketRepository;
    private readonly IEmployeRepository _employeRepository;
    private readonly IHammamRepository _hammamRepository;

    public TicketService(
        ITicketRepository ticketRepository,
        ITypeTicketRepository typeTicketRepository,
        IEmployeRepository employeRepository,
        IHammamRepository hammamRepository)
    {
        _ticketRepository = ticketRepository;
        _typeTicketRepository = typeTicketRepository;
        _employeRepository = employeRepository;
        _hammamRepository = hammamRepository;
    }

    public async Task<TicketDto?> GetByIdAsync(Guid id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        return ticket == null ? null : MapToDto(ticket);
    }

    public async Task<IEnumerable<TicketDto>> GetAllAsync(Guid? hammamId = null, Guid? employeId = null, DateTime? from = null, DateTime? to = null)
    {
        // Si un hammamId est spécifié et un employeId aussi, filtrer par les deux
        if (hammamId.HasValue && employeId.HasValue)
        {
            var hammamTickets = await _ticketRepository.GetByHammamIdAsync(hammamId.Value, from, to);
            return hammamTickets
                .Where(t => t.EmployeId == employeId.Value)
                .Select(MapToDto);
        }

        if (hammamId.HasValue)
        {
            var tickets = await _ticketRepository.GetByHammamIdAsync(hammamId.Value, from, to);
            return tickets.Select(MapToDto);
        }

        if (employeId.HasValue)
        {
            var tickets = await _ticketRepository.GetByEmployeIdAsync(employeId.Value, from, to);
            return tickets.Select(MapToDto);
        }

        // Pas de filtre spécifique, utiliser la plage de dates
        if (from.HasValue && to.HasValue)
        {
            var tickets = await _ticketRepository.GetByDateRangeAsync(from.Value, to.Value);
            return tickets.Select(MapToDto);
        }

        // Par défaut, tickets d'aujourd'hui
        var moroccoOffset = TimeSpan.FromHours(1);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = nowUtc + moroccoOffset;
        var todayLocalStart = nowLocal.Date;
        var todayUtcStart = todayLocalStart - moroccoOffset;
        var tomorrowUtcStart = todayUtcStart.AddDays(1);
        
        var todayTickets = await _ticketRepository.GetByDateRangeAsync(todayUtcStart, tomorrowUtcStart);
        return todayTickets.Select(MapToDto);
    }

    public async Task<IEnumerable<TicketDto>> GetByHammamAsync(Guid hammamId, DateTime? from = null, DateTime? to = null)
    {
        var tickets = await _ticketRepository.GetByHammamIdAsync(hammamId, from, to);
        return tickets.Select(MapToDto);
    }

    public async Task<IEnumerable<TicketDto>> GetByEmployeAsync(Guid employeId, DateTime? from = null, DateTime? to = null)
    {
        var tickets = await _ticketRepository.GetByEmployeIdAsync(employeId, from, to);
        return tickets.Select(MapToDto);
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request)
    {
        var ticket = new Ticket
        {
            Id = request.Id,
            TypeTicketId = request.TypeTicketId,
            EmployeId = request.EmployeId,
            HammamId = request.HammamId,
            Prix = request.Prix,
            CreatedAt = request.CreatedAt,
            SyncedAt = DateTime.UtcNow,
            SyncStatus = SyncStatus.Synced,
            DeviceId = request.DeviceId
        };

        var created = await _ticketRepository.AddAsync(ticket);
        return MapToDto(created);
    }

    /// <summary>
    /// Synchronisation massive de tickets depuis l'application desktop
    /// Gère les conflits et les doublons
    /// </summary>
    public async Task<SyncTicketsResponse> SyncTicketsAsync(SyncTicketsRequest request)
    {
        var inserted = 0;
        var updated = 0;
        var errors = 0;
        var failedIds = new List<Guid>();

        foreach (var ticketRequest in request.Tickets)
        {
            try
            {
                // Vérifier si le ticket existe déjà
                var existing = await _ticketRepository.GetByIdAsync(ticketRequest.Id);

                if (existing == null)
                {
                    // Nouveau ticket - insertion
                    var ticket = new Ticket
                    {
                        Id = ticketRequest.Id,
                        TypeTicketId = ticketRequest.TypeTicketId,
                        EmployeId = ticketRequest.EmployeId,
                        HammamId = ticketRequest.HammamId,
                        Prix = ticketRequest.Prix,
                        CreatedAt = ticketRequest.CreatedAt,
                        SyncedAt = DateTime.UtcNow,
                        SyncStatus = SyncStatus.Synced,
                        DeviceId = ticketRequest.DeviceId
                    };

                    await _ticketRepository.AddAsync(ticket);
                    inserted++;
                }
                else
                {
                    // Ticket existant - comparaison des dates
                    // Le plus récent gagne
                    if (ticketRequest.CreatedAt > existing.CreatedAt)
                    {
                        existing.Prix = ticketRequest.Prix;
                        existing.TypeTicketId = ticketRequest.TypeTicketId;
                        existing.SyncedAt = DateTime.UtcNow;
                        existing.SyncStatus = SyncStatus.Synced;

                        await _ticketRepository.UpdateAsync(existing);
                        updated++;
                    }
                    else
                    {
                        // Ticket local plus ancien, on garde celui du serveur
                        updated++; // Compté comme "traité"
                    }
                }
            }
            catch
            {
                errors++;
                failedIds.Add(ticketRequest.Id);
            }
        }

        return new SyncTicketsResponse(
            TotalReceived: request.Tickets.Count(),
            Inserted: inserted,
            Updated: updated,
            Errors: errors,
            FailedTicketIds: failedIds
        );
    }

    public async Task<int> GetTodayCountAsync(Guid? hammamId = null, Guid? employeId = null)
    {
        // Fuseau horaire du Maroc (UTC+1)
        var moroccoOffset = TimeSpan.FromHours(1);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = nowUtc + moroccoOffset;
        var todayLocalStart = nowLocal.Date;
        var todayUtcStart = todayLocalStart - moroccoOffset;
        var tomorrowUtcStart = todayUtcStart.AddDays(1);

        if (hammamId.HasValue)
        {
            var tickets = await _ticketRepository.GetByHammamIdAsync(hammamId.Value, todayUtcStart, tomorrowUtcStart);
            return tickets.Count();
        }

        if (employeId.HasValue)
        {
            var tickets = await _ticketRepository.GetByEmployeIdAsync(employeId.Value, todayUtcStart, tomorrowUtcStart);
            return tickets.Count();
        }

        var allTickets = await _ticketRepository.GetByDateRangeAsync(todayUtcStart, tomorrowUtcStart);
        return allTickets.Count();
    }

    public async Task<decimal> GetTodayRevenueAsync(Guid? hammamId = null, Guid? employeId = null)
    {
        // Fuseau horaire du Maroc (UTC+1)
        var moroccoOffset = TimeSpan.FromHours(1);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = nowUtc + moroccoOffset;
        var todayLocalStart = nowLocal.Date;
        var todayUtcStart = todayLocalStart - moroccoOffset;
        var tomorrowUtcStart = todayUtcStart.AddDays(1);

        if (hammamId.HasValue)
        {
            var tickets = await _ticketRepository.GetByHammamIdAsync(hammamId.Value, todayUtcStart, tomorrowUtcStart);
            return tickets.Sum(t => t.Prix);
        }

        if (employeId.HasValue)
        {
            var tickets = await _ticketRepository.GetByEmployeIdAsync(employeId.Value, todayUtcStart, tomorrowUtcStart);
            return tickets.Sum(t => t.Prix);
        }

        var allTickets = await _ticketRepository.GetByDateRangeAsync(todayUtcStart, tomorrowUtcStart);
        return allTickets.Sum(t => t.Prix);
    }

    private static TicketDto MapToDto(Ticket ticket)
    {
        return new TicketDto(
            Id: ticket.Id,
            TypeTicketId: ticket.TypeTicketId,
            TypeTicketNom: ticket.TypeTicket?.Nom ?? "",
            EmployeId: ticket.EmployeId,
            EmployeNom: ticket.Employe != null ? $"{ticket.Employe.Prenom} {ticket.Employe.Nom}" : "",
            HammamId: ticket.HammamId,
            HammamNom: ticket.Hammam?.Nom ?? "",
            Prix: ticket.Prix,
            CreatedAt: ticket.CreatedAt,
            SyncedAt: ticket.SyncedAt,
            SyncStatus: ticket.SyncStatus.ToString()
        );
    }
}
