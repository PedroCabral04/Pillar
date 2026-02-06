using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface IAccountReceivableDao
{
    Task<AccountReceivable?> GetByIdAsync(int id);
    Task<AccountReceivable?> GetByIdWithRelationsAsync(int id);
    Task<List<AccountReceivable>> GetAllAsync();
    Task<(List<AccountReceivable> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? customerId = null,
        AccountStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int? categoryId = null,
        int? costCenterId = null,
        string? sortBy = null,
        bool sortDescending = false,
        string? searchText = null);
    Task<List<AccountReceivable>> GetOverdueAsync();
    Task<List<AccountReceivable>> GetDueSoonAsync(int days = 7);
    Task<decimal> GetTotalByStatusAsync(AccountStatus status);
    Task<decimal> GetTotalByCustomerAsync(int customerId);
    Task<List<AccountReceivable>> GetInstallmentsAsync(int parentAccountId);
    Task<AccountReceivable> CreateAsync(AccountReceivable account);
    Task<AccountReceivable> UpdateAsync(AccountReceivable account);
    Task DeleteAsync(int id);
}

public class AccountReceivableDao : IAccountReceivableDao
{
    private readonly ApplicationDbContext _context;

    public AccountReceivableDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccountReceivable?> GetByIdAsync(int id)
    {
        return await _context.AccountsReceivable
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AccountReceivable?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.AccountsReceivable
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Category)
            .Include(a => a.CostCenter)
            .Include(a => a.CreatedByUser)
            .Include(a => a.ReceivedByUser)
            .Include(a => a.Installments)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<AccountReceivable>> GetAllAsync()
    {
        return await _context.AccountsReceivable
            .AsNoTracking()
            .Include(a => a.Customer)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<AccountReceivable> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? customerId = null,
        AccountStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int? categoryId = null,
        int? costCenterId = null,
        string? sortBy = null,
        bool sortDescending = false,
        string? searchText = null)
    {
        var query = _context.AccountsReceivable
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Category)
            .Include(a => a.CostCenter)
            .AsQueryable();

        // Apply filters
        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        else
            query = query.Where(a => a.Status != AccountStatus.Cancelled);

        if (dueDateFrom.HasValue)
            query = query.Where(a => a.DueDate >= dueDateFrom.Value);

        if (dueDateTo.HasValue)
            query = query.Where(a => a.DueDate <= dueDateTo.Value);

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        if (costCenterId.HasValue)
            query = query.Where(a => a.CostCenterId == costCenterId.Value);

        // Apply text search
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(a => 
                (a.InvoiceNumber != null && a.InvoiceNumber.ToLower().Contains(search)) ||
                (a.Customer != null && a.Customer.Name.ToLower().Contains(search)) ||
                (a.Notes != null && a.Notes.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "duedate" => sortDescending ? query.OrderByDescending(a => a.DueDate) : query.OrderBy(a => a.DueDate),
            "amount" => sortDescending ? query.OrderByDescending(a => a.OriginalAmount) : query.OrderBy(a => a.OriginalAmount),
            "customer" => sortDescending ? query.OrderByDescending(a => a.Customer!.Name) : query.OrderBy(a => a.Customer!.Name),
            "status" => sortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => query.OrderByDescending(a => a.DueDate)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<AccountReceivable>> GetOverdueAsync()
    {
        return await _context.AccountsReceivable
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.DueDate < DateTime.UtcNow && a.Status == AccountStatus.Pending)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<List<AccountReceivable>> GetDueSoonAsync(int days = 7)
    {
        var targetDate = DateTime.UtcNow.AddDays(days);
        return await _context.AccountsReceivable
            .AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.DueDate >= DateTime.UtcNow && a.DueDate <= targetDate && a.Status == AccountStatus.Pending)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalByStatusAsync(AccountStatus status)
    {
        return await _context.AccountsReceivable
            .Where(a => a.Status == status)
            .SumAsync(a => a.OriginalAmount - a.DiscountAmount + a.InterestAmount + a.FineAmount);
    }

    public async Task<decimal> GetTotalByCustomerAsync(int customerId)
    {
        return await _context.AccountsReceivable
            .Where(a => a.CustomerId == customerId && a.Status != AccountStatus.Paid)
            .SumAsync(a => a.OriginalAmount - a.DiscountAmount + a.InterestAmount + a.FineAmount - a.PaidAmount);
    }

    public async Task<List<AccountReceivable>> GetInstallmentsAsync(int parentAccountId)
    {
        return await _context.AccountsReceivable
            .AsNoTracking()
            .Where(a => a.ParentAccountId == parentAccountId)
            .OrderBy(a => a.InstallmentNumber)
            .ToListAsync();
    }

    public async Task<AccountReceivable> CreateAsync(AccountReceivable account)
    {
        _context.AccountsReceivable.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<AccountReceivable> UpdateAsync(AccountReceivable account)
    {
        _context.AccountsReceivable.Update(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _context.AccountsReceivable.FindAsync(id);
        if (account != null)
        {
            _context.AccountsReceivable.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
