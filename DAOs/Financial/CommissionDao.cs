using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface ICommissionDao
{
    Task<Commission?> GetByIdAsync(int id);
    Task<List<Commission>> GetBySaleIdAsync(int saleId);
    Task<List<Commission>> GetByUserIdAsync(int userId);
    Task<List<Commission>> GetByUserIdAndMonthAsync(int userId, int year, int month);
    Task<List<Commission>> GetAllAsync(int? userId = null, CommissionStatus? status = null, int? year = null, int? month = null);
    Task<Commission> CreateAsync(Commission commission);
    Task<Commission> UpdateAsync(Commission commission);
    Task DeleteAsync(int id);
}

public class CommissionDao : ICommissionDao
{
    private readonly ApplicationDbContext _context;

    public CommissionDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Commission?> GetByIdAsync(int id)
    {
        return await _context.Commissions
            .Include(c => c.Sale)
            .Include(c => c.SaleItem)
            .Include(c => c.Product)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Commission>> GetBySaleIdAsync(int saleId)
    {
        return await _context.Commissions
            .Include(c => c.Product)
            .Include(c => c.User)
            .Where(c => c.SaleId == saleId)
            .ToListAsync();
    }

    public async Task<List<Commission>> GetByUserIdAsync(int userId)
    {
        return await _context.Commissions
            .Include(c => c.Sale)
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Commission>> GetByUserIdAndMonthAsync(int userId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        return await _context.Commissions
            .Include(c => c.Sale)
            .Include(c => c.Product)
            .Where(c => c.UserId == userId &&
                       c.CreatedAt >= startDate &&
                       c.CreatedAt < endDate)
            .ToListAsync();
    }

    public async Task<List<Commission>> GetAllAsync(int? userId = null, CommissionStatus? status = null, int? year = null, int? month = null)
    {
        var query = _context.Commissions
            .Include(c => c.Sale)
            .Include(c => c.SaleItem)
            .Include(c => c.Product)
            .Include(c => c.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (year.HasValue && month.HasValue)
        {
            var startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);
            query = query.Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate);
        }
        else if (year.HasValue)
        {
            var startDate = new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddYears(1);
            query = query.Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Commission> CreateAsync(Commission commission)
    {
        _context.Commissions.Add(commission);
        await _context.SaveChangesAsync();
        return commission;
    }

    public async Task<Commission> UpdateAsync(Commission commission)
    {
        _context.Commissions.Update(commission);
        await _context.SaveChangesAsync();
        return commission;
    }

    public async Task DeleteAsync(int id)
    {
        var commission = await _context.Commissions.FindAsync(id);
        if (commission != null)
        {
            _context.Commissions.Remove(commission);
            await _context.SaveChangesAsync();
        }
    }
}
