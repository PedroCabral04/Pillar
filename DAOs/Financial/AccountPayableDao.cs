using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface IAccountPayableDao
{
    Task<AccountPayable?> GetByIdAsync(int id);
    Task<AccountPayable?> GetByIdWithRelationsAsync(int id);
    Task<List<AccountPayable>> GetAllAsync();
    Task<(List<AccountPayable> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? supplierId = null,
        AccountStatus? status = null,
        bool? requiresApproval = null,
        bool? pendingApproval = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int? categoryId = null,
        int? costCenterId = null,
        string? sortBy = null,
        bool sortDescending = false);
    Task<List<AccountPayable>> GetOverdueAsync();
    Task<List<AccountPayable>> GetDueSoonAsync(int days = 7);
    Task<List<AccountPayable>> GetPendingApprovalAsync();
    Task<decimal> GetTotalByStatusAsync(AccountStatus status);
    Task<decimal> GetTotalBySupplierAsync(int supplierId);
    Task<List<AccountPayable>> GetInstallmentsAsync(int parentAccountId);
    Task<AccountPayable> CreateAsync(AccountPayable account);
    Task<AccountPayable> UpdateAsync(AccountPayable account);
    Task DeleteAsync(int id);
}

public class AccountPayableDao : IAccountPayableDao
{
    private readonly ApplicationDbContext _context;

    public AccountPayableDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccountPayable?> GetByIdAsync(int id)
    {
        return await _context.AccountsPayable
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AccountPayable?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.AccountsPayable
            .AsNoTracking()
            .Include(a => a.Supplier)
            .Include(a => a.Category)
            .Include(a => a.CostCenter)
            .Include(a => a.CreatedByUser)
            .Include(a => a.PaidByUser)
            .Include(a => a.ApprovedByUser)
            .Include(a => a.Installments)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<AccountPayable>> GetAllAsync()
    {
        return await _context.AccountsPayable
            .AsNoTracking()
            .Include(a => a.Supplier)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<AccountPayable> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? supplierId = null,
        AccountStatus? status = null,
        bool? requiresApproval = null,
        bool? pendingApproval = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int? categoryId = null,
        int? costCenterId = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = _context.AccountsPayable
            .AsNoTracking()
            .Include(a => a.Supplier)
            .Include(a => a.Category)
            .Include(a => a.CostCenter)
            .AsQueryable();

        // Apply filters
        if (supplierId.HasValue)
            query = query.Where(a => a.SupplierId == supplierId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (requiresApproval.HasValue)
            query = query.Where(a => a.RequiresApproval == requiresApproval.Value);

        if (pendingApproval.HasValue && pendingApproval.Value)
            query = query.Where(a => a.RequiresApproval && a.ApprovalDate == null);

        if (dueDateFrom.HasValue)
            query = query.Where(a => a.DueDate >= dueDateFrom.Value);

        if (dueDateTo.HasValue)
            query = query.Where(a => a.DueDate <= dueDateTo.Value);

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        if (costCenterId.HasValue)
            query = query.Where(a => a.CostCenterId == costCenterId.Value);

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "duedate" => sortDescending ? query.OrderByDescending(a => a.DueDate) : query.OrderBy(a => a.DueDate),
            "amount" => sortDescending ? query.OrderByDescending(a => a.OriginalAmount) : query.OrderBy(a => a.OriginalAmount),
            "supplier" => sortDescending ? query.OrderByDescending(a => a.Supplier!.Name) : query.OrderBy(a => a.Supplier!.Name),
            "status" => sortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => query.OrderByDescending(a => a.DueDate)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<AccountPayable>> GetOverdueAsync()
    {
        return await _context.AccountsPayable
            .AsNoTracking()
            .Include(a => a.Supplier)
            .Where(a => a.DueDate < DateTime.UtcNow && a.Status == AccountStatus.Pending)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<List<AccountPayable>> GetDueSoonAsync(int days = 7)
    {
        var targetDate = DateTime.UtcNow.AddDays(days);
        return await _context.AccountsPayable
            .AsNoTracking()
            .Include(a => a.Supplier)
            .Where(a => a.DueDate >= DateTime.UtcNow && a.DueDate <= targetDate && a.Status == AccountStatus.Pending)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<List<AccountPayable>> GetPendingApprovalAsync()
    {
        return await _context.AccountsPayable
            .AsNoTracking()
            .Include(a => a.Supplier)
            .Where(a => a.RequiresApproval && a.ApprovalDate == null)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalByStatusAsync(AccountStatus status)
    {
        return await _context.AccountsPayable
            .Where(a => a.Status == status)
            .SumAsync(a => a.OriginalAmount - a.DiscountAmount + a.InterestAmount + a.FineAmount);
    }

    public async Task<decimal> GetTotalBySupplierAsync(int supplierId)
    {
        return await _context.AccountsPayable
            .Where(a => a.SupplierId == supplierId && a.Status != AccountStatus.Paid)
            .SumAsync(a => a.OriginalAmount - a.DiscountAmount + a.InterestAmount + a.FineAmount - a.PaidAmount);
    }

    public async Task<List<AccountPayable>> GetInstallmentsAsync(int parentAccountId)
    {
        return await _context.AccountsPayable
            .AsNoTracking()
            .Where(a => a.ParentAccountId == parentAccountId)
            .OrderBy(a => a.InstallmentNumber)
            .ToListAsync();
    }

    public async Task<AccountPayable> CreateAsync(AccountPayable account)
    {
        _context.AccountsPayable.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<AccountPayable> UpdateAsync(AccountPayable account)
    {
        _context.AccountsPayable.Update(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _context.AccountsPayable.FindAsync(id);
        if (account != null)
        {
            _context.AccountsPayable.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
