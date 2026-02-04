namespace HammamAPI.Application.DTOs;

// ==================== AUTH DTOs ====================

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    EmployeDto Employe
);

public record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Profil employé pour l'écran de login (sans mot de passe)
/// </summary>
public class EmployeProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Icone { get; set; } = "User1";
    public Guid HammamId { get; set; }
    public string HammamNom { get; set; } = "";
}

// ==================== EMPLOYE DTOs ====================

public record EmployeDto(
    Guid Id,
    string Username,
    string Nom,
    string Prenom,
    Guid HammamId,
    string HammamNom,
    string HammamNomArabe,
    int HammamPrefixeTicket,
    string Langue,
    string Role,
    bool Actif,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record CreateEmployeRequest(
    string Username,
    string Password,
    string Nom,
    string Prenom,
    Guid HammamId,
    string Langue = "FR",
    string Role = "Employe"
);

public record UpdateEmployeRequest(
    string? Nom,
    string? Prenom,
    Guid? HammamId,
    string? Langue,
    bool? Actif
);

public record ResetPasswordRequest(
    Guid EmployeId
);

public record ResetPasswordResponse(
    string NewPassword
);

// ==================== HAMMAM DTOs ====================

public record HammamDto(
    Guid Id,
    string Code,
    string Nom,
    string Adresse,
    bool Actif,
    int NombreEmployes,
    int NombreTickets,
    decimal Revenue,
    IEnumerable<TypeTicketDto> TypeTickets,
    DateTime CreatedAt
);

public record HammamSimpleDto(
    Guid Id,
    string Code,
    string Nom,
    string Adresse,
    bool Actif,
    int NombreEmployes,
    DateTime CreatedAt
);

public record CreateHammamRequest(
    string Code,
    string Nom,
    string Adresse,
    List<CreateTypeTicketRequest>? TypeTickets,
    List<CreateEmployeRequest>? Employes
);

public record UpdateHammamRequest(
    string? Nom,
    string? Adresse,
    bool? Actif
);

// ==================== TYPE TICKET DTOs ====================

public record TypeTicketDto(
    Guid Id,
    string Nom,
    decimal Prix,
    string Couleur,
    string Icone,
    int Ordre,
    bool Actif,
    Guid? HammamId
);

public record CreateTypeTicketRequest(
    string Nom,
    decimal Prix,
    string Couleur = "#3B82F6",
    string Icone = "User",
    int Ordre = 0,
    Guid? HammamId = null
);

public record UpdateTypeTicketRequest(
    string? Nom,
    decimal? Prix,
    string? Couleur,
    string? Icone,
    int? Ordre,
    bool? Actif
);

// ==================== TICKET DTOs ====================

public record TicketDto(
    Guid Id,
    Guid TypeTicketId,
    string TypeTicketNom,
    Guid EmployeId,
    string EmployeNom,
    Guid HammamId,
    string HammamNom,
    decimal Prix,
    DateTime CreatedAt,
    DateTime? SyncedAt,
    string SyncStatus
);

public record CreateTicketRequest(
    Guid Id, // UUID généré localement
    Guid TypeTicketId,
    Guid EmployeId,
    Guid HammamId,
    decimal Prix,
    DateTime CreatedAt,
    string? DeviceId
);

public record SyncTicketsRequest(
    IEnumerable<CreateTicketRequest> Tickets
);

public record SyncTicketsResponse(
    int TotalReceived,
    int Inserted,
    int Updated,
    int Errors,
    IEnumerable<Guid> FailedTicketIds
);

// ==================== STATS DTOs ====================

public record DashboardStatsDto(
    int TotalTicketsToday,
    decimal TotalRevenueToday,
    int HammamsActifs,
    decimal VariationTickets,  // Pourcentage vs période précédente
    decimal VariationRevenue,  // Pourcentage vs période précédente
    IEnumerable<HammamStatsDto> HammamStats,
    IEnumerable<EmployeStatsDto> EmployeStats
);

public record HammamStatsDto(
    Guid HammamId,
    string HammamNom,
    int TicketsCount,
    decimal Revenue,
    decimal RevenueAttendu,
    decimal Ecart,
    decimal EcartPourcentage,
    bool HasAlert // true si écart > 5%
);

public record EmployeStatsDto(
    Guid EmployeId,
    string EmployeNom,
    string HammamNom,
    int TicketsCount,
    decimal Revenue,
    int Classement
);

public record PeriodFilter(
    DateTime From,
    DateTime To
);

// ==================== RAPPORT DTOs ====================

public record RapportRequest(
    string Type, // journalier, mensuel, personnalise
    DateTime From,
    DateTime To,
    IEnumerable<Guid>? HammamIds,
    IEnumerable<Guid>? EmployeIds
);

public record RapportPreviewDto(
    int TotalTickets,
    decimal TotalRevenue,
    DateTime PeriodeDebut,
    DateTime PeriodeFin,
    IEnumerable<RapportLigneDto> LignesParHammam,
    IEnumerable<RapportLigneDto> LignesParEmploye,
    IEnumerable<RapportLigneDto> LignesParType,
    IEnumerable<RapportJournalierDto> LignesParJour
);

public record RapportLigneDto(
    string Label,
    int TicketsCount,
    decimal Revenue,
    decimal Pourcentage
);

public record RapportJournalierDto(
    DateTime Date,
    int TicketsCount,
    decimal Revenue
);
