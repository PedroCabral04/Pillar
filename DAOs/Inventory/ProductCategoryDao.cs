using erp.Data;
using erp.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Inventory;

public class ProductCategoryDao : IProductCategoryDao
{
    private readonly ApplicationDbContext _context;

    public ProductCategoryDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductCategory>> GetAllAsync()
    {
        return await _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ProductCategory?> GetByIdAsync(int id)
    {
        return await _context.ProductCategories.FindAsync(id);
    }

    public async Task<ProductCategory?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ProductCategory> CreateAsync(ProductCategory category)
    {
        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<ProductCategory> UpdateAsync(ProductCategory category)
    {
        _context.ProductCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category == null) return false;

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
}
