using erp.DTOs.Inventory;
using erp.Models.Inventory;

namespace erp.Services.Inventory;

public interface IInventoryService
{
    // Product operations
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<(IEnumerable<ProductDto> Products, int TotalCount)> SearchProductsAsync(ProductSearchDto search);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, int userId);
    Task<ProductDto> UpdateProductAsync(UpdateProductDto dto);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> BulkUpdatePricesAsync(BulkUpdatePriceDto dto);
    
    // Category operations
    Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync();
    Task<ProductCategoryDto?> GetCategoryByIdAsync(int id);
    
    // Stock operations
    Task<decimal> GetProductStockAsync(int productId, int? warehouseId = null);
    Task<bool> ValidateStockAvailabilityAsync(int productId, decimal quantity, int? warehouseId = null);
    
    // Alert operations
    Task<IEnumerable<StockAlertDto>> GetStockAlertsAsync();
    Task<IEnumerable<StockAlertDto>> GetLowStockProductsAsync();
    Task<IEnumerable<StockAlertDto>> GetOverstockProductsAsync();
    Task<IEnumerable<StockAlertDto>> GetInactiveProductsAsync(int daysInactive = 90);
}
