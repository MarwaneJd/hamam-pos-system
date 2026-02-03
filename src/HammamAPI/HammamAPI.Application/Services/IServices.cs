using HammamAPI.Application.DTOs;

namespace HammamAPI.Application.Services;

/// <summary>
/// Service d'authentification et de gestion des tokens JWT
/// </summary>
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(Guid employeId);
    Task<bool> ValidateTokenAsync(string token);
    string GenerateRandomPassword(int length = 10);
}

/// <summary>
/// Service de gestion des employés
/// </summary>
public interface IEmployeService
{
    Task<EmployeDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EmployeDto>> GetAllAsync();
    Task<IEnumerable<EmployeDto>> GetByHammamAsync(Guid hammamId);
    Task<EmployeDto> CreateAsync(CreateEmployeRequest request);
    Task<EmployeDto?> UpdateAsync(Guid id, UpdateEmployeRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<ResetPasswordResponse> ResetPasswordAsync(Guid id);
    Task<bool> ActivateAsync(Guid id, bool actif);
}

/// <summary>
/// Service de gestion des hammams
/// </summary>
public interface IHammamService
{
    Task<HammamDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<HammamDto>> GetAllAsync();
    Task<IEnumerable<HammamDto>> GetAllActiveAsync();
    Task<HammamDto> CreateAsync(CreateHammamRequest request);
    Task<HammamDto?> UpdateAsync(Guid id, UpdateHammamRequest request);
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>
/// Service de gestion des tickets et synchronisation
/// </summary>
public interface ITicketService
{
    Task<TicketDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TicketDto>> GetByHammamAsync(Guid hammamId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<TicketDto>> GetByEmployeAsync(Guid employeId, DateTime? from = null, DateTime? to = null);
    Task<TicketDto> CreateAsync(CreateTicketRequest request);
    Task<SyncTicketsResponse> SyncTicketsAsync(SyncTicketsRequest request);
    Task<int> GetTodayCountAsync(Guid? hammamId = null, Guid? employeId = null);
    Task<decimal> GetTodayRevenueAsync(Guid? hammamId = null, Guid? employeId = null);
}

/// <summary>
/// Service des statistiques et dashboard
/// </summary>
public interface IStatsService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<HammamStatsDto>> GetHammamStatsAsync(DateTime from, DateTime to);
    Task<IEnumerable<EmployeStatsDto>> GetEmployeStatsAsync(DateTime from, DateTime to);
    Task<decimal> CalculerEcartAsync(Guid hammamId, DateTime date);
}

/// <summary>
/// Service de génération de rapports
/// </summary>
public interface IRapportService
{
    Task<RapportPreviewDto> PreviewAsync(RapportRequest request);
    Task<byte[]> GenerateExcelAsync(RapportRequest request);
    Task<byte[]> GeneratePdfAsync(RapportRequest request);
    Task<byte[]> GenerateCsvAsync(RapportRequest request);
}

/// <summary>
/// Service des types de tickets
/// </summary>
public interface ITypeTicketService
{
    Task<IEnumerable<TypeTicketDto>> GetAllAsync();
    Task<TypeTicketDto?> GetByIdAsync(Guid id);
}
