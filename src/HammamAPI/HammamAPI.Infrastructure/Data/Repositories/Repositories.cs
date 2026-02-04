using HammamAPI.Domain.Entities;
using HammamAPI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.Infrastructure.Data.Repositories;

/// <summary>
/// Repository générique de base
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly HammamDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(HammamDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.FindAsync(id) != null;
    }
}

/// <summary>
/// Repository spécifique pour les Hammams
/// </summary>
public class HammamRepository : Repository<Hammam>, IHammamRepository
{
    public HammamRepository(HammamDbContext context) : base(context)
    {
    }

    public async Task<Hammam?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(h => h.Code == code);
    }

    public async Task<IEnumerable<Hammam>> GetAllActiveAsync()
    {
        return await _dbSet.Where(h => h.Actif).ToListAsync();
    }
}

/// <summary>
/// Repository spécifique pour les Employés
/// </summary>
public class EmployeRepository : Repository<Employe>, IEmployeRepository
{
    public EmployeRepository(HammamDbContext context) : base(context)
    {
    }

    public override async Task<Employe?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(e => e.Hammam)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employe?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(e => e.Hammam)
            .FirstOrDefaultAsync(e => e.Username == username);
    }

    public async Task<IEnumerable<Employe>> GetAllByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(e => e.Hammam)
            .Where(e => e.Username == username && e.Actif)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employe>> GetByHammamIdAsync(Guid hammamId)
    {
        return await _dbSet
            .Include(e => e.Hammam)
            .Where(e => e.HammamId == hammamId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employe>> GetAllActiveAsync()
    {
        return await _dbSet
            .Include(e => e.Hammam)
            .Where(e => e.Actif)
            .ToListAsync();
    }

    public async Task UpdateLastLoginAsync(Guid id)
    {
        var employe = await GetByIdAsync(id);
        if (employe != null)
        {
            employe.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

/// <summary>
/// Repository spécifique pour les Types de Tickets
/// </summary>
public class TypeTicketRepository : Repository<TypeTicket>, ITypeTicketRepository
{
    public TypeTicketRepository(HammamDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TypeTicket>> GetAllActiveOrderedAsync()
    {
        return await _dbSet
            .Where(t => t.Actif)
            .OrderBy(t => t.Ordre)
            .ToListAsync();
    }
}

/// <summary>
/// Repository spécifique pour les Tickets
/// </summary>
public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(HammamDbContext context) : base(context)
    {
    }

    public override async Task<Ticket?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(t => t.TypeTicket)
            .Include(t => t.Employe)
            .Include(t => t.Hammam)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Ticket>> GetByHammamIdAsync(Guid hammamId, DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet
            .Include(t => t.TypeTicket)
            .Include(t => t.Employe)
            .Include(t => t.Hammam)
            .Where(t => t.HammamId == hammamId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt < to.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByEmployeIdAsync(Guid employeId, DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet
            .Include(t => t.TypeTicket)
            .Include(t => t.Employe)
            .Include(t => t.Hammam)
            .Where(t => t.EmployeId == employeId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt < to.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet
            .Include(t => t.TypeTicket)
            .Include(t => t.Employe)
            .Include(t => t.Hammam)
            .Where(t => t.CreatedAt >= from && t.CreatedAt < to)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetUnsyncedAsync()
    {
        return await _dbSet
            .Where(t => t.SyncStatus == SyncStatus.Pending)
            .ToListAsync();
    }

    public async Task BulkInsertAsync(IEnumerable<Ticket> tickets)
    {
        await _dbSet.AddRangeAsync(tickets);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsSyncedAsync(IEnumerable<Guid> ticketIds)
    {
        var tickets = await _dbSet
            .Where(t => ticketIds.Contains(t.Id))
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            ticket.SyncStatus = SyncStatus.Synced;
            ticket.SyncedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetCountByDateAsync(Guid hammamId, DateTime date)
    {
        var from = date.Date;
        var to = from.AddDays(1);

        return await _dbSet
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= from && t.CreatedAt < to)
            .CountAsync();
    }

    public async Task<decimal> GetRevenueByDateAsync(Guid hammamId, DateTime date)
    {
        var from = date.Date;
        var to = from.AddDays(1);

        return await _dbSet
            .Where(t => t.HammamId == hammamId && t.CreatedAt >= from && t.CreatedAt < to)
            .SumAsync(t => t.Prix);
    }
}
