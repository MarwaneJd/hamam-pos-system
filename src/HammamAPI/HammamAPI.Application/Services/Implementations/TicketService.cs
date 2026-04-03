using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using HammamAPI.Domain.Entities;
using HammamAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        ITypeTicketRepository typeTicketRepository,
        IEmployeRepository employeRepository,
        IHammamRepository hammamRepository,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _typeTicketRepository = typeTicketRepository;
        _employeRepository = employeRepository;
        _hammamRepository = hammamRepository;
        _logger = logger;
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
            TicketNumber = request.TicketNumber,
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
    public async Task<SyncTicketsResponse> SyncTicketsAsync(SyncTicketsRequest request, Guid? callerHammamId = null)
    {
        var inserted = 0;
        var updated = 0;
        var errors = 0;
        var failedIds = new List<Guid>();

        // Caches pour éviter les requêtes répétées
        var employeIdCache = new Dictionary<Guid, Guid>();
        var hammamIdCache = new Dictionary<Guid, Guid>();
        var typeTicketIdCache = new Dictionary<Guid, Guid>();

        // Pré-charger tous les types de tickets du serveur pour le matching par nom
        var allTypeTickets = (await _typeTicketRepository.GetAllActiveOrderedAsync()).ToList();

        foreach (var ticketRequest in request.Tickets)
        {
            try
            {
                // === 1. Résoudre le HammamId ===
                Guid resolvedHammamId;
                if (!hammamIdCache.TryGetValue(ticketRequest.HammamId, out resolvedHammamId))
                {
                    if (await _hammamRepository.ExistsAsync(ticketRequest.HammamId))
                    {
                        resolvedHammamId = ticketRequest.HammamId;
                    }
                    else if (callerHammamId.HasValue && await _hammamRepository.ExistsAsync(callerHammamId.Value))
                    {
                        resolvedHammamId = callerHammamId.Value;
                        _logger.LogWarning("[SYNC] HammamId {TicketHammamId} introuvable, remplacé par HammamId du caller {CallerHammamId}", ticketRequest.HammamId, callerHammamId.Value);
                    }
                    else
                    {
                        // Rejeter le ticket — ne pas assigner à un hammam arbitraire
                        _logger.LogError("[SYNC] HammamId {TicketHammamId} introuvable et aucun caller HammamId valide. Ticket {TicketId} rejeté.", ticketRequest.HammamId, ticketRequest.Id);
                        errors++;
                        failedIds.Add(ticketRequest.Id);
                        continue;
                    }
                    hammamIdCache[ticketRequest.HammamId] = resolvedHammamId;
                }

                // === 2. Résoudre le TypeTicketId ===
                Guid resolvedTypeTicketId;
                if (!typeTicketIdCache.TryGetValue(ticketRequest.TypeTicketId, out resolvedTypeTicketId))
                {
                    var typeExists = allTypeTickets.Any(t => t.Id == ticketRequest.TypeTicketId);
                    if (typeExists)
                    {
                        resolvedTypeTicketId = ticketRequest.TypeTicketId;
                    }
                    else
                    {
                        // Chercher un type de ticket du même hammam, ou global, par ordre
                        // D'abord les types spécifiques au hammam, puis les globaux
                        var hammamTypes = allTypeTickets.Where(t => t.HammamId == resolvedHammamId).ToList();
                        var globalTypes = allTypeTickets.Where(t => t.HammamId == null).ToList();
                        var candidateTypes = hammamTypes.Any() ? hammamTypes : globalTypes;

                        // Essayer de deviner par le prix (le plus proche)
                        var matchByPrice = candidateTypes
                            .OrderBy(t => Math.Abs(t.Prix - ticketRequest.Prix))
                            .FirstOrDefault();

                        if (matchByPrice != null)
                        {
                            resolvedTypeTicketId = matchByPrice.Id;
                            _logger.LogWarning("[SYNC] TypeTicketId {TicketTypeId} introuvable, remplacé par {ResolvedTypeId} ({TypeNom}, prix={TypePrix}) via correspondance de prix", ticketRequest.TypeTicketId, matchByPrice.Id, matchByPrice.Nom, matchByPrice.Prix);
                        }
                        else
                        {
                            // Dernier recours : premier type disponible
                            var anyType = allTypeTickets.FirstOrDefault();
                            if (anyType != null)
                            {
                                resolvedTypeTicketId = anyType.Id;
                                _logger.LogWarning("[SYNC] TypeTicketId {TicketTypeId} introuvable, fallback vers {ResolvedTypeId} ({TypeNom})", ticketRequest.TypeTicketId, anyType.Id, anyType.Nom);
                            }
                            else
                            {
                                _logger.LogError("[SYNC] Aucun type de ticket trouvé pour remplacer {TicketTypeId}. Ticket {TicketId} rejeté.", ticketRequest.TypeTicketId, ticketRequest.Id);
                                errors++;
                                failedIds.Add(ticketRequest.Id);
                                continue;
                            }
                        }
                    }
                    typeTicketIdCache[ticketRequest.TypeTicketId] = resolvedTypeTicketId;
                }

                // === 3. Résoudre l'EmployeId (en utilisant le HammamId RÉSOLU) ===
                Guid resolvedEmployeId;
                if (!employeIdCache.TryGetValue(ticketRequest.EmployeId, out resolvedEmployeId))
                {
                    if (await _employeRepository.ExistsAsync(ticketRequest.EmployeId))
                    {
                        resolvedEmployeId = ticketRequest.EmployeId;
                    }
                    else
                    {
                        // Chercher un employé actif du MÊME hammam (utiliser le HammamId RÉSOLU, pas celui du ticket)
                        var hammamEmployes = await _employeRepository.GetByHammamIdAsync(resolvedHammamId);
                        var fallback = hammamEmployes.FirstOrDefault(e => e.Actif);
                        if (fallback != null)
                        {
                            resolvedEmployeId = fallback.Id;
                            _logger.LogWarning("[SYNC] EmployeId {TicketEmployeId} introuvable, remplacé par {ResolvedEmployeId} ({EmpPrenom} {EmpNom})", ticketRequest.EmployeId, fallback.Id, fallback.Prenom, fallback.Nom);
                        }
                        else
                        {
                            // Prendre n'importe quel employé du hammam
                            var anyEmploye = hammamEmployes.FirstOrDefault();
                            if (anyEmploye != null)
                            {
                                resolvedEmployeId = anyEmploye.Id;
                                _logger.LogWarning("[SYNC] EmployeId {TicketEmployeId} introuvable, fallback vers {ResolvedEmployeId} ({EmpPrenom} {EmpNom})", ticketRequest.EmployeId, anyEmploye.Id, anyEmploye.Prenom, anyEmploye.Nom);
                            }
                            else
                            {
                                _logger.LogError("[SYNC] Aucun employé trouvé pour hammam {HammamId}. Ticket {TicketId} rejeté.", resolvedHammamId, ticketRequest.Id);
                                errors++;
                                failedIds.Add(ticketRequest.Id);
                                continue;
                            }
                        }
                    }
                    employeIdCache[ticketRequest.EmployeId] = resolvedEmployeId;
                }

                // === Cross-validation : le HammamId de l'employé est la source de vérité ===
                var resolvedEmploye = await _employeRepository.GetByIdAsync(resolvedEmployeId);
                if (resolvedEmploye != null && resolvedEmploye.HammamId != resolvedHammamId)
                {
                    _logger.LogWarning(
                        "[SYNC] Cross-validation: ticket HammamId={TicketHammam} mais employé {EmpId} appartient à HammamId={EmpHammam}. Utilisation du hammam de l'employé.",
                        resolvedHammamId, resolvedEmployeId, resolvedEmploye.HammamId);
                    resolvedHammamId = resolvedEmploye.HammamId;
                    hammamIdCache[ticketRequest.HammamId] = resolvedHammamId;
                }

                // === 4. Insérer ou mettre à jour le ticket ===
                var existing = await _ticketRepository.GetByIdAsync(ticketRequest.Id);

                if (existing == null)
                {
                    var createdAtUtc = ticketRequest.CreatedAt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(ticketRequest.CreatedAt, DateTimeKind.Utc)
                        : ticketRequest.CreatedAt.ToUniversalTime();

                    var ticket = new Ticket
                    {
                        Id = ticketRequest.Id,
                        TicketNumber = ticketRequest.TicketNumber,
                        TypeTicketId = resolvedTypeTicketId,
                        EmployeId = resolvedEmployeId,
                        HammamId = resolvedHammamId,
                        Prix = ticketRequest.Prix,
                        CreatedAt = createdAtUtc,
                        SyncedAt = DateTime.UtcNow,
                        SyncStatus = SyncStatus.Synced,
                        DeviceId = ticketRequest.DeviceId
                    };

                    await _ticketRepository.AddAsync(ticket);
                    inserted++;
                }
                else
                {
                    if (ticketRequest.CreatedAt > existing.CreatedAt)
                    {
                        existing.TicketNumber = ticketRequest.TicketNumber;
                        existing.Prix = ticketRequest.Prix;
                        existing.TypeTicketId = resolvedTypeTicketId;
                        existing.SyncedAt = DateTime.UtcNow;
                        existing.SyncStatus = SyncStatus.Synced;

                        await _ticketRepository.UpdateAsync(existing);
                        updated++;
                    }
                    else
                    {
                        updated++; // Compté comme "traité"
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SYNC] Erreur ticket {TicketId} EmployeId={EmployeId} HammamId={HammamId} TypeTicketId={TypeTicketId}", ticketRequest.Id, ticketRequest.EmployeId, ticketRequest.HammamId, ticketRequest.TypeTicketId);
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

    /// <summary>
    /// Compte le total de TOUS les tickets d'un hammam (depuis toujours)
    /// Utilisé pour le compteur permanent de numéros de tickets
    /// </summary>
    public async Task<int> GetTotalCountAsync(Guid? hammamId = null)
    {
        return await _ticketRepository.GetTotalCountAsync(hammamId);
    }

    private static TicketDto MapToDto(Ticket ticket)
    {
        return new TicketDto(
            Id: ticket.Id,
            TicketNumber: ticket.TicketNumber,
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
