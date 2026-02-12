using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface IVendorPerformanceDao
{
    Task<VendorPerformance?> GetByUserAndPeriodAsync(int userId, int year, int month);
    Task<List<VendorPerformance>> GetByTenantAndPeriodAsync(int tenantId, int year, int month);
    Task<VendorPerformance> CreateAsync(VendorPerformance performance);
    Task<VendorPerformance> UpdateAsync(VendorPerformance performance);
    Task<List<VendorPerformance>> GetTopPerformersAsync(int tenantId, int year, int month, int topN);
}

public class VendorPerformanceDao : IVendorPerformanceDao
{
    private readonly ApplicationDbContext _context;

    public VendorPerformanceDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VendorPerformance?> GetByUserAndPeriodAsync(int userId, int year, int month)
    {
        return await _context.VendorPerformances
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Year == year && p.Month == month);
    }

    public async Task<List<VendorPerformance>> GetByTenantAndPeriodAsync(int tenantId, int year, int month)
    {
        return await _context.VendorPerformances
            .Include(p => p.User)
            .Where(p => p.TenantId == tenantId && p.Year == year && p.Month == month)
            .OrderByDescending(p => p.TotalCommissionEarned)
            .ToListAsync();
    }

    public async Task<List<VendorPerformance>> GetTopPerformersAsync(int tenantId, int year, int month, int topN)
    {
        return await _context.VendorPerformances
            .Include(p => p.User)
            .Where(p => p.TenantId == tenantId && p.Year == year && p.Month == month)
            .OrderByDescending(p => p.TotalSalesAmount)
            .Take(topN)
            .ToListAsync();
    }

    public async Task<VendorPerformance> CreateAsync(VendorPerformance performance)
    {
        _context.VendorPerformances.Add(performance);
        await _context.SaveChangesAsync();
        return performance;
    }

    public async Task<VendorPerformance> UpdateAsync(VendorPerformance performance)
    {
        _context.VendorPerformances.Update(performance);
        await _context.SaveChangesAsync();
        return performance;
    }
}
