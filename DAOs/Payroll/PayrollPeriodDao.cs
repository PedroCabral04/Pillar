using erp.Models.TimeTracking;
using erp.Data;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Payroll;

public class PayrollPeriodDao : IPayrollPeriodDao
{
    private readonly ApplicationDbContext _context;

    public PayrollPeriodDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollPeriod?> GetByIdAsync(int id)
    {
        return await _context.PayrollPeriods
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PayrollPeriod?> GetByPeriodAsync(int year, int month)
    {
        return await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.ReferenceYear == year && p.ReferenceMonth == month);
    }

    public async Task<List<PayrollPeriod>> GetAllAsync()
    {
        return await _context.PayrollPeriods
            .OrderByDescending(p => p.ReferenceYear)
            .ThenByDescending(p => p.ReferenceMonth)
            .ToListAsync();
    }

    public async Task<PayrollPeriod> CreateAsync(PayrollPeriod period)
    {
        _context.PayrollPeriods.Add(period);
        await _context.SaveChangesAsync();
        return period;
    }

    public async Task<PayrollPeriod> UpdateAsync(PayrollPeriod period)
    {
        _context.PayrollPeriods.Update(period);
        await _context.SaveChangesAsync();
        return period;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var period = await _context.PayrollPeriods.FindAsync(id);
        if (period == null) return false;

        _context.PayrollPeriods.Remove(period);
        await _context.SaveChangesAsync();
        return true;
    }
}
