using HammamAPI.Domain.Entities;
using HammamAPI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.Infrastructure.Data.Repositories;

/// <summary>
/// Implémentation du repository pour les Versements
/// </summary>
public class VersementRepository : IVersementRepository
{
    private readonly HammamDbContext _context;

    public VersementRepository(HammamDbContext context)
    {
        _context = context;
    }

    public async Task<Versement?> GetByIdAsync(Guid id)
    {
        return await _context.Versements
            .Include(v => v.Employe)
            .Include(v => v.Hammam)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Versement>> GetAllAsync()
    {
        return await _context.Versements
            .Include(v => v.Employe)
            .Include(v => v.Hammam)
            .OrderByDescending(v => v.DateVersement)
            .ToListAsync();
    }

    public async Task<Versement> AddAsync(Versement entity)
    {
        await _context.Versements.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Versement> UpdateAsync(Versement entity)
    {
        _context.Versements.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(Guid id)
    {
        var versement = await _context.Versements.FindAsync(id);
        if (versement != null)
        {
            _context.Versements.Remove(versement);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Versements.AnyAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Versement>> GetByHammamIdAsync(Guid hammamId, DateTime from, DateTime to)
    {
        return await _context.Versements
            .Include(v => v.Employe)
            .Where(v => v.HammamId == hammamId && v.DateVersement >= from && v.DateVersement < to)
            .OrderByDescending(v => v.DateVersement)
            .ToListAsync();
    }

    public async Task<IEnumerable<Versement>> GetByEmployeIdAsync(Guid employeId, DateTime from, DateTime to)
    {
        return await _context.Versements
            .Where(v => v.EmployeId == employeId && v.DateVersement >= from && v.DateVersement < to)
            .OrderByDescending(v => v.DateVersement)
            .ToListAsync();
    }

    public async Task<Versement?> GetByEmployeDateAsync(Guid employeId, DateTime date)
    {
        var dateStart = date.Date;
        var dateEnd = dateStart.AddDays(1);
        
        return await _context.Versements
            .FirstOrDefaultAsync(v => v.EmployeId == employeId && v.DateVersement >= dateStart && v.DateVersement < dateEnd);
    }

    public async Task<decimal> GetTotalRemisAsync(Guid hammamId, DateTime from, DateTime to)
    {
        return await _context.Versements
            .Where(v => v.HammamId == hammamId && v.DateVersement >= from && v.DateVersement < to)
            .SumAsync(v => v.MontantRemis);
    }
}
