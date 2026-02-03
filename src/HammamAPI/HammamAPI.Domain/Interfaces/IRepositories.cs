using HammamAPI.Domain.Entities;

namespace HammamAPI.Domain.Interfaces;

/// <summary>
/// Interface générique pour les opérations CRUD de base
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}

/// <summary>
/// Repository spécifique pour les Hammams
/// </summary>
public interface IHammamRepository : IRepository<Hammam>
{
    Task<Hammam?> GetByCodeAsync(string code);
    Task<IEnumerable<Hammam>> GetAllActiveAsync();
}

/// <summary>
/// Repository spécifique pour les Employés
/// </summary>
public interface IEmployeRepository : IRepository<Employe>
{
    Task<Employe?> GetByUsernameAsync(string username);
    Task<IEnumerable<Employe>> GetByHammamIdAsync(Guid hammamId);
    Task<IEnumerable<Employe>> GetAllActiveAsync();
    Task UpdateLastLoginAsync(Guid id);
}

/// <summary>
/// Repository spécifique pour les Types de Tickets
/// </summary>
public interface ITypeTicketRepository : IRepository<TypeTicket>
{
    Task<IEnumerable<TypeTicket>> GetAllActiveOrderedAsync();
}

/// <summary>
/// Repository spécifique pour les Tickets
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    Task<IEnumerable<Ticket>> GetByHammamIdAsync(Guid hammamId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<Ticket>> GetByEmployeIdAsync(Guid employeId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<Ticket>> GetUnsyncedAsync();
    Task BulkInsertAsync(IEnumerable<Ticket> tickets);
    Task MarkAsSyncedAsync(IEnumerable<Guid> ticketIds);
    Task<int> GetCountByDateAsync(Guid hammamId, DateTime date);
    Task<decimal> GetRevenueByDateAsync(Guid hammamId, DateTime date);
}

/// <summary>
/// Repository spécifique pour les Versements
/// </summary>
public interface IVersementRepository : IRepository<Versement>
{
    Task<IEnumerable<Versement>> GetByHammamIdAsync(Guid hammamId, DateTime from, DateTime to);
    Task<IEnumerable<Versement>> GetByEmployeIdAsync(Guid employeId, DateTime from, DateTime to);
    Task<Versement?> GetByEmployeDateAsync(Guid employeId, DateTime date);
    Task<decimal> GetTotalRemisAsync(Guid hammamId, DateTime from, DateTime to);
}
