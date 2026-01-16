using erp.Models.Inventory;

namespace erp.DAOs.Inventory;

public interface IStockMovementDao
{
    Task<StockMovement?> GetByIdAsync(int id);
    Task<List<StockMovement>> GetByProductAsync(int productId);
    Task<List<StockMovement>> GetByProductPagedAsync(int productId, int page, int pageSize);
    Task<StockMovement> CreateAsync(StockMovement movement);
    Task<StockMovement> UpdateAsync(StockMovement movement);
    Task<bool> DeleteAsync(int id);
    Task<DateTime?> GetLastMovementDateAsync(int productId);
}
