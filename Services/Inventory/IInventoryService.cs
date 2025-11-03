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
    Task<(IEnumerable<ProductCategoryDto> Categories, int TotalCount)> GetCategoriesAsync(
        string? search = null, 
        int? parentCategoryId = null, 
        bool? isActive = null, 
        int page = 1, 
        int pageSize = 20);
    Task<ProductCategoryDto?> GetCategoryByIdAsync(int id);
    Task<ProductCategoryDto> CreateCategoryAsync(CreateProductCategoryDto dto);
    Task<ProductCategoryDto> UpdateCategoryAsync(UpdateProductCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
    
    // Stock Movement operations
    Task<(IEnumerable<StockMovementDto> Movements, int TotalCount)> GetStockMovementsAsync(
        int? productId = null,
        string? movementType = null,
        int? warehouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20);
    Task<StockMovementDto?> GetStockMovementByIdAsync(int id);
    Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementDto dto, int userId);
    
    // Warehouse operations
    Task<(IEnumerable<WarehouseDto> Warehouses, int TotalCount)> GetWarehousesAsync(
        bool? isActive = null,
        int page = 1,
        int pageSize = 100);
    
    // Alert operations
    Task<IEnumerable<StockAlertDto>> GetStockAlertsAsync();
    Task<IEnumerable<StockAlertDto>> GetLowStockProductsAsync();
    Task<IEnumerable<StockAlertDto>> GetOverstockProductsAsync();
    Task<IEnumerable<StockAlertDto>> GetInactiveProductsAsync(int daysInactive = 90);
}
