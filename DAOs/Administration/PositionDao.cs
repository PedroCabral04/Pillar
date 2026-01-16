using erp.Data;
using erp.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Administration;

public class PositionDao : IPositionDao
{
    private readonly ApplicationDbContext _context;

    public PositionDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Position?> GetByIdAsync(int id)
    {
        return await _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Position?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Positions
            .AsNoTracking()
            .Include(p => p.DefaultDepartment)
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Position>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.Positions
            .Include(p => p.DefaultDepartment)
            .Include(p => p.Employees)
            .AsNoTracking();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Level)
            .ThenBy(p => p.Title)
            .ToListAsync();
    }

    public async Task<Position?> GetByCodeAsync(string code)
    {
        return await _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _context.Positions.Where(p => p.Code == code);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<Position> CreateAsync(Position position)
    {
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
        return position;
    }

    public async Task<Position> UpdateAsync(Position position)
    {
        _context.Positions.Update(position);
        await _context.SaveChangesAsync();
        return position;
    }

    public async Task DeleteAsync(int id)
    {
        var position = await _context.Positions.FindAsync(id);
        if (position != null)
        {
            _context.Positions.Remove(position);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasEmployeesAsync(int id)
    {
        return await _context.Positions
            .Where(p => p.Id == id)
            .AnyAsync(p => p.Employees.Any());
    }
}
