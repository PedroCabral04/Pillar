using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface ICostCenterDao
{
    Task<CostCenter?> GetByIdAsync(int id);
    Task<CostCenter?> GetByIdWithRelationsAsync(int id);
    Task<List<CostCenter>> GetAllAsync(bool activeOnly = true);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<CostCenter> CreateAsync(CostCenter costCenter);
    Task<CostCenter> UpdateAsync(CostCenter costCenter);
    Task DeleteAsync(int id);
}

public class CostCenterDao : ICostCenterDao
{
    private readonly ApplicationDbContext _context;

    public CostCenterDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CostCenter?> GetByIdAsync(int id)
    {
        return await _context.CostCenters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CostCenter?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.CostCenters
            .AsNoTracking()
            .Include(c => c.Manager)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<CostCenter>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.CostCenters.AsNoTracking();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _context.CostCenters.Where(c => c.Code == code);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<CostCenter> CreateAsync(CostCenter costCenter)
    {
        _context.CostCenters.Add(costCenter);
        await _context.SaveChangesAsync();
        return costCenter;
    }

    public async Task<CostCenter> UpdateAsync(CostCenter costCenter)
    {
        _context.CostCenters.Update(costCenter);
        await _context.SaveChangesAsync();
        return costCenter;
    }

    public async Task DeleteAsync(int id)
    {
        var costCenter = await _context.CostCenters.FindAsync(id);
        if (costCenter != null)
        {
            _context.CostCenters.Remove(costCenter);
            await _context.SaveChangesAsync();
        }
    }
}
