using erp.DTOs.Inventory;
using erp.Data;
using erp.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Inventory;

public class ProductDao : IProductDao
{
    private readonly ApplicationDbContext _context;

    public ProductDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Suppliers)
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku);
    }

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
    {
        var query = _context.Products.Where(p => p.Sku == sku);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null)
    {
        var query = _context.Products.Where(p => p.Barcode == barcode);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<(List<Product> Items, int TotalCount)> SearchAsync(ProductSearchDto search)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.CreatedByUser)
            .AsNoTracking()
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            var term = search.SearchTerm.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        if (search.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == search.CategoryId.Value);

        if (search.BrandId.HasValue)
            query = query.Where(p => p.BrandId == search.BrandId.Value);

        if (search.Status.HasValue)
            query = query.Where(p => (int)p.Status == search.Status.Value);

        if (search.IsActive.HasValue)
            query = query.Where(p => p.IsActive == search.IsActive.Value);

        if (search.LowStock == true)
            query = query.Where(p => p.CurrentStock <= p.MinimumStock);

        var totalCount = await query.CountAsync();

        // Ordenação
        query = search.SortBy?.ToLower() switch
        {
            "name" => search.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "sku" => search.SortDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            "price" => search.SortDescending ? query.OrderByDescending(p => p.SalePrice) : query.OrderBy(p => p.SalePrice),
            "stock" => search.SortDescending ? query.OrderByDescending(p => p.CurrentStock) : query.OrderBy(p => p.CurrentStock),
            _ => query.OrderBy(p => p.Name)
        };

        // Paginação
        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Product>> GetForBulkUpdateAsync(List<int> productIds)
    {
        return await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasStockMovementsAsync(int productId)
    {
        return await _context.StockMovements.AnyAsync(m => m.ProductId == productId);
    }

    public async Task<decimal> GetStockAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        return product?.CurrentStock ?? 0;
    }

    public async Task<List<Product>> GetLowStockAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.CurrentStock <= p.ReorderPoint)
            .ToListAsync();
    }

    public async Task<List<Product>> GetOverstockAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.CurrentStock > p.MaximumStock && p.MaximumStock > 0)
            .ToListAsync();
    }
}
