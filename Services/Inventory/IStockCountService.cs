using erp.DTOs.Inventory;
using erp.Models.Inventory;

namespace erp.Services.Inventory;

public interface IStockCountService
{
    Task<(IEnumerable<StockCountDto> Counts, int TotalCount)> GetCountsAsync(
        string? status = null,
        int? warehouseId = null,
        int page = 1,
        int pageSize = 20);
    Task<StockCountDto> CreateCountAsync(CreateStockCountDto dto, int userId);
    Task<StockCountDto?> GetCountByIdAsync(int id);
    Task<IEnumerable<StockCountDto>> GetActiveCountsAsync();
    Task<StockCountDto> AddItemToCountAsync(AddStockCountItemDto dto);
    Task<StockCountDto> ApproveCountAsync(ApproveStockCountDto dto, int userId);
    Task<bool> CancelCountAsync(int countId);
    Task<string> GenerateCountNumberAsync();
}
