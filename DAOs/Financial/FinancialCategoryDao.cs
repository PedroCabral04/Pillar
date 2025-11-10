using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface IFinancialCategoryDao
{
    Task<FinancialCategory?> GetByIdAsync(int id);
    Task<FinancialCategory?> GetByIdWithRelationsAsync(int id);
    Task<List<FinancialCategory>> GetAllAsync(bool activeOnly = true);
    Task<List<FinancialCategory>> GetByTypeAsync(CategoryType type, bool activeOnly = true);
    Task<List<FinancialCategory>> GetRootCategoriesAsync(CategoryType? type = null);
    Task<List<FinancialCategory>> GetSubCategoriesAsync(int parentId);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<FinancialCategory> CreateAsync(FinancialCategory category);
    Task<FinancialCategory> UpdateAsync(FinancialCategory category);
    Task DeleteAsync(int id);
}

public class FinancialCategoryDao : IFinancialCategoryDao
{
    private readonly ApplicationDbContext _context;

    public FinancialCategoryDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialCategory?> GetByIdAsync(int id)
    {
        return await _context.FinancialCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<FinancialCategory?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.FinancialCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<FinancialCategory>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.FinancialCategories.AsNoTracking();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<List<FinancialCategory>> GetByTypeAsync(CategoryType type, bool activeOnly = true)
    {
        var query = _context.FinancialCategories
            .AsNoTracking()
            .Where(c => c.Type == type);

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<List<FinancialCategory>> GetRootCategoriesAsync(CategoryType? type = null)
    {
        var query = _context.FinancialCategories
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == null && c.IsActive);

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        return await query
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<List<FinancialCategory>> GetSubCategoriesAsync(int parentId)
    {
        return await _context.FinancialCategories
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == parentId && c.IsActive)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _context.FinancialCategories.Where(c => c.Code == code);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<FinancialCategory> CreateAsync(FinancialCategory category)
    {
        _context.FinancialCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<FinancialCategory> UpdateAsync(FinancialCategory category)
    {
        _context.FinancialCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.FinancialCategories.FindAsync(id);
        if (category != null)
        {
            _context.FinancialCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
