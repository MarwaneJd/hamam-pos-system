using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using HammamAPI.Domain.Interfaces;

namespace HammamAPI.Application.Services.Implementations;

/// <summary>
/// Service des statistiques pour le dashboard admin
/// </summary>
public class StatsService : IStatsService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IHammamRepository _hammamRepository;
    private readonly IEmployeRepository _employeRepository;
    private readonly ITypeTicketRepository _typeTicketRepository;
    private readonly IVersementRepository _versementRepository;

    public StatsService(
        ITicketRepository ticketRepository,
        IHammamRepository hammamRepository,
        IEmployeRepository employeRepository,
        ITypeTicketRepository typeTicketRepository,
        IVersementRepository versementRepository)
    {
        _ticketRepository = ticketRepository;
        _hammamRepository = hammamRepository;
        _employeRepository = employeRepository;
        _typeTicketRepository = typeTicketRepository;
        _versementRepository = versementRepository;
    }

    /// <summary>
    /// Récupère toutes les statistiques pour le dashboard
    /// </summary>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(DateTime? from = null, DateTime? to = null)
    {
        var fromDate = from ?? DateTime.UtcNow.Date;
        var toDate = to ?? DateTime.UtcNow.Date.AddDays(1);

        // Stats globales pour la période demandée
        var allTickets = await _ticketRepository.GetByDateRangeAsync(fromDate, toDate);
        var ticketsList = allTickets.ToList();
        var totalTickets = ticketsList.Count;
        var totalRevenue = ticketsList.Sum(t => t.Prix);

        // Calcul de la période précédente pour les variations
        var periodDuration = toDate - fromDate;
        var previousFrom = fromDate.Subtract(periodDuration);
        var previousTo = fromDate;
        
        var previousTickets = await _ticketRepository.GetByDateRangeAsync(previousFrom, previousTo);
        var previousTicketsList = previousTickets.ToList();
        var previousTotalTickets = previousTicketsList.Count;
        var previousTotalRevenue = previousTicketsList.Sum(t => t.Prix);

        // Calcul des variations en pourcentage
        var variationTickets = previousTotalTickets > 0 
            ? Math.Round(((decimal)(totalTickets - previousTotalTickets) / previousTotalTickets) * 100, 1) 
            : 0;
        var variationRevenue = previousTotalRevenue > 0 
            ? Math.Round(((totalRevenue - previousTotalRevenue) / previousTotalRevenue) * 100, 1) 
            : 0;

        // Hammams actifs
        var hammams = await _hammamRepository.GetAllActiveAsync();
        var hammamsActifs = hammams.Count();

        // Stats par hammam
        var hammamStats = await GetHammamStatsAsync(fromDate, toDate);

        // Stats par employé (classement)
        var employeStats = await GetEmployeStatsAsync(fromDate, toDate);

        return new DashboardStatsDto(
            TotalTicketsToday: totalTickets,
            TotalRevenueToday: totalRevenue,
            HammamsActifs: hammamsActifs,
            VariationTickets: variationTickets,
            VariationRevenue: variationRevenue,
            HammamStats: hammamStats,
            EmployeStats: employeStats
        );
    }

    /// <summary>
    /// Statistiques par hammam avec calcul des écarts basé sur les versements
    /// </summary>
    public async Task<IEnumerable<HammamStatsDto>> GetHammamStatsAsync(DateTime from, DateTime to)
    {
        var hammams = await _hammamRepository.GetAllActiveAsync();
        var stats = new List<HammamStatsDto>();

        foreach (var hammam in hammams)
        {
            var tickets = await _ticketRepository.GetByHammamIdAsync(hammam.Id, from, to);
            var ticketsList = tickets.ToList();

            var ticketsCount = ticketsList.Count;
            var revenue = ticketsList.Sum(t => t.Prix); // Montant théorique (ce que les employés devraient remettre)

            // Récupérer le total des versements (montants remis par les employés)
            var totalRemis = await _versementRepository.GetTotalRemisAsync(hammam.Id, from, to);

            // Écart = Montant remis - Montant théorique
            // Positif = les employés ont remis plus que prévu
            // Négatif = les employés ont remis moins que prévu (déficit)
            var ecart = totalRemis - revenue;
            var ecartPourcentage = revenue > 0 
                ? (ecart / revenue) * 100 
                : 0;

            stats.Add(new HammamStatsDto(
                HammamId: hammam.Id,
                HammamNom: hammam.Nom,
                TicketsCount: ticketsCount,
                Revenue: revenue,
                RevenueAttendu: revenue, // Le montant théorique est basé sur les tickets
                Ecart: ecart,
                EcartPourcentage: Math.Round(ecartPourcentage, 2),
                HasAlert: Math.Abs(ecartPourcentage) > 5 // Alerte si écart > 5%
            ));
        }

        return stats.OrderByDescending(s => s.Revenue);
    }

    /// <summary>
    /// Statistiques par employé avec classement
    /// </summary>
    public async Task<IEnumerable<EmployeStatsDto>> GetEmployeStatsAsync(DateTime from, DateTime to)
    {
        var employes = await _employeRepository.GetAllActiveAsync();
        var stats = new List<EmployeStatsDto>();

        foreach (var employe in employes)
        {
            var tickets = await _ticketRepository.GetByEmployeIdAsync(employe.Id, from, to);
            var ticketsList = tickets.ToList();

            stats.Add(new EmployeStatsDto(
                EmployeId: employe.Id,
                EmployeNom: $"{employe.Prenom} {employe.Nom}",
                HammamNom: employe.Hammam?.Nom ?? "",
                TicketsCount: ticketsList.Count,
                Revenue: ticketsList.Sum(t => t.Prix),
                Classement: 0 // Sera calculé après le tri
            ));
        }

        // Trier par revenus décroissants et attribuer les classements
        var sortedStats = stats
            .OrderByDescending(s => s.Revenue)
            .Select((s, index) => new EmployeStatsDto(
                EmployeId: s.EmployeId,
                EmployeNom: s.EmployeNom,
                HammamNom: s.HammamNom,
                TicketsCount: s.TicketsCount,
                Revenue: s.Revenue,
                Classement: index + 1
            ))
            .ToList();

        return sortedStats;
    }

    /// <summary>
    /// Calcule l'écart de caisse pour un hammam à une date donnée
    /// </summary>
    public async Task<decimal> CalculerEcartAsync(Guid hammamId, DateTime date)
    {
        var from = date.Date;
        var to = from.AddDays(1);

        var tickets = await _ticketRepository.GetByHammamIdAsync(hammamId, from, to);
        var typeTickets = await _typeTicketRepository.GetAllActiveOrderedAsync();
        var prixStandard = typeTickets.ToDictionary(t => t.Id, t => t.Prix);

        var ticketsList = tickets.ToList();
        var revenueReel = ticketsList.Sum(t => t.Prix);
        var revenueAttendu = ticketsList.Sum(t =>
            prixStandard.TryGetValue(t.TypeTicketId, out var prix) ? prix : t.Prix
        );

        return revenueReel - revenueAttendu;
    }
}
