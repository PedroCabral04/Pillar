using erp.DTOs.Inventory;
using erp.Models.Inventory;

namespace erp.DAOs.Inventory;

public interface IProductDao
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByIdWithRelationsAsync(int id);
    Task<Product?> GetBySkuAsync(string sku);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null);
    Task<(List<Product> Items, int TotalCount)> SearchAsync(ProductSearchDto search);
    Task<List<Product>> GetForBulkUpdateAsync(List<int> productIds);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasStockMovementsAsync(int productId);
    Task<decimal> GetStockAsync(int productId);
    Task<List<Product>> GetLowStockAsync();
    Task<List<Product>> GetOverstockAsync();
}
