using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface IEmployeePaymentDao
{
    Task<(List<EmployeePayment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? employeeId = null, EmployeePaymentType? type = null,
        AccountStatus? status = null, int? referenceMonth = null, int? referenceYear = null,
        string? sortBy = null, bool sortDescending = false);

    Task<EmployeePayment?> GetByIdAsync(int id);
    Task<EmployeePayment> CreateAsync(EmployeePayment payment);
    Task<EmployeePayment> UpdateAsync(EmployeePayment payment);
    Task DeleteAsync(EmployeePayment payment);
    Task<List<EmployeePayment>> GetByEmployeeAndPeriodAsync(int employeeId, int month, int year);
    Task<decimal> GetTotalByStatusAsync(AccountStatus status);
}

public class EmployeePaymentDao : IEmployeePaymentDao
{
    private readonly ApplicationDbContext _context;

    public EmployeePaymentDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<EmployeePayment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? employeeId = null, EmployeePaymentType? type = null,
        AccountStatus? status = null, int? referenceMonth = null, int? referenceYear = null,
        string? sortBy = null, bool sortDescending = false)
    {
        var query = _context.EmployeePayments
            .Include(e => e.Employee)
            .Include(e => e.Category)
            .Include(e => e.CostCenter)
            .Include(e => e.ServiceOrder)
            .Include(e => e.CreatedByUser)
            .Include(e => e.PaidByUser)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId.Value);

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (referenceMonth.HasValue)
            query = query.Where(e => e.ReferenceMonth == referenceMonth.Value);

        if (referenceYear.HasValue)
            query = query.Where(e => e.ReferenceYear == referenceYear.Value);

        query = sortBy?.ToLower() switch
        {
            "employee" => sortDescending
                ? query.OrderByDescending(e => e.Employee.FullName)
                : query.OrderBy(e => e.Employee.FullName),
            "duedate" => sortDescending
                ? query.OrderByDescending(e => e.DueDate)
                : query.OrderBy(e => e.DueDate),
            "amount" => sortDescending
                ? query.OrderByDescending(e => e.NetAmount)
                : query.OrderBy(e => e.NetAmount),
            "status" => sortDescending
                ? query.OrderByDescending(e => e.Status)
                : query.OrderBy(e => e.Status),
            _ => sortDescending
                ? query.OrderByDescending(e => e.CreatedAt)
                : query.OrderByDescending(e => e.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<EmployeePayment?> GetByIdAsync(int id)
    {
        return await _context.EmployeePayments
            .Include(e => e.Employee)
            .Include(e => e.Category)
            .Include(e => e.CostCenter)
            .Include(e => e.ServiceOrder)
            .Include(e => e.CreatedByUser)
            .Include(e => e.PaidByUser)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EmployeePayment> CreateAsync(EmployeePayment payment)
    {
        _context.EmployeePayments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<EmployeePayment> UpdateAsync(EmployeePayment payment)
    {
        _context.EmployeePayments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task DeleteAsync(EmployeePayment payment)
    {
        _context.EmployeePayments.Remove(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<List<EmployeePayment>> GetByEmployeeAndPeriodAsync(int employeeId, int month, int year)
    {
        return await _context.EmployeePayments
            .Where(e => e.EmployeeId == employeeId
                     && e.ReferenceMonth == month
                     && e.ReferenceYear == year)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalByStatusAsync(AccountStatus status)
    {
        return await _context.EmployeePayments
            .Where(e => e.Status == status)
            .SumAsync(e => e.NetAmount);
    }
}
