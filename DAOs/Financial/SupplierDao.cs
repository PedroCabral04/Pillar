using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface ISupplierDao
{
    Task<Supplier?> GetByIdAsync(int id);
    Task<Supplier?> GetByIdWithRelationsAsync(int id);
    Task<List<Supplier>> GetAllAsync(bool activeOnly = true);
    Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? search = null, 
        bool? activeOnly = null,
        string? sortBy = null,
        bool sortDescending = false);
    Task<Supplier?> GetByTaxIdAsync(string taxId);
    Task<bool> TaxIdExistsAsync(string taxId, int? excludeId = null);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<Supplier> UpdateAsync(Supplier supplier);
    Task DeleteAsync(int id);
}

public class SupplierDao : ISupplierDao
{
    private readonly ApplicationDbContext _context;

    public SupplierDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.CreatedByUser)
            .Include(s => s.Category)
            .Include(s => s.AccountsPayable)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Supplier>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.Suppliers.AsNoTracking();

        if (activeOnly)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = _context.Suppliers
            .Include(s => s.Category)
            .AsNoTracking();

        // Apply filters
        if (activeOnly.HasValue)
            query = query.Where(s => s.IsActive == activeOnly.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search) ||
                (s.TradeName != null && s.TradeName.ToLower().Contains(search)) ||
                s.TaxId.Contains(search) ||
                (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "taxid" => sortDescending ? query.OrderByDescending(s => s.TaxId) : query.OrderBy(s => s.TaxId),
            "createdat" => sortDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Name)
        };

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Supplier?> GetByTaxIdAsync(string taxId)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TaxId == taxId);
    }

    public async Task<bool> TaxIdExistsAsync(string taxId, int? excludeId = null)
    {
        var query = _context.Suppliers.Where(s => s.TaxId == taxId);

        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
        }
    }
}
