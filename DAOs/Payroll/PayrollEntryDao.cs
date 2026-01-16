using erp.Models.TimeTracking;
using erp.Data;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Payroll;

public class PayrollEntryDao : IPayrollEntryDao
{
    private readonly ApplicationDbContext _context;

    public PayrollEntryDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollEntry?> GetByIdAsync(int id)
    {
        return await _context.PayrollEntries
            .Include(e => e.PayrollPeriod)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<PayrollEntry>> GetByPeriodAsync(int periodId)
    {
        return await _context.PayrollEntries
            .Include(e => e.Employee)
            .Where(e => e.PayrollPeriodId == periodId)
            .OrderBy(e => e.Employee.FullName)
            .ToListAsync();
    }

    public async Task<PayrollEntry> CreateAsync(PayrollEntry entry)
    {
        _context.PayrollEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<PayrollEntry> UpdateAsync(PayrollEntry entry)
    {
        _context.PayrollEntries.Update(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entry = await _context.PayrollEntries.FindAsync(id);
        if (entry == null) return false;

        _context.PayrollEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PayrollEntry>> GetByEmployeeAsync(int employeeId, int year, int month)
    {
        return await _context.PayrollEntries
            .Include(e => e.PayrollPeriod)
            .Where(e => e.EmployeeId == employeeId && e.PayrollPeriod.ReferenceYear == year && e.PayrollPeriod.ReferenceMonth == month)
            .ToListAsync();
    }
}
