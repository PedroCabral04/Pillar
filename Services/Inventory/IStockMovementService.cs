using erp.DTOs.Inventory;
using erp.Models.Inventory;

namespace erp.Services.Inventory;

public interface IStockMovementService
{
    Task<StockMovementDto> CreateMovementAsync(CreateStockMovementDto dto, int userId);
    Task<StockMovementDto> CreateEntryAsync(int productId, decimal quantity, decimal unitCost, string? documentNumber, string? notes, int userId, int? warehouseId = null);
    Task<StockMovementDto> CreateExitAsync(int productId, decimal quantity, string? documentNumber, string? notes, int userId, int? warehouseId = null);
    Task<StockMovementDto> CreateAdjustmentAsync(int productId, decimal newStock, string reason, int userId, int? warehouseId = null);
    Task<IEnumerable<StockMovementDto>> GetMovementsByProductAsync(int productId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<StockMovementDto>> GetMovementsByDateRangeAsync(DateTime startDate, DateTime endDate, int? warehouseId = null);
    Task<StockMovementDto?> GetMovementByIdAsync(int id);
}
