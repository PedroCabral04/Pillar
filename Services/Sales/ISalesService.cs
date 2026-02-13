using erp.DTOs.Sales;

namespace erp.Services.Sales;

public interface ISalesService
{
    Task<SaleDto> CreateAsync(CreateSaleDto dto, int userId, CancellationToken ct = default);
    Task<SaleDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<SaleDto> items, int total)> SearchAsync(
        string? search,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<SaleDto> UpdateAsync(int id, UpdateSaleDto dto, CancellationToken ct = default);
    Task<bool> CancelAsync(int id, CancellationToken ct = default);
    Task<SaleDto> FinalizeAsync(int id, CancellationToken ct = default);
    Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<(string productName, decimal quantity)>> GetTopProductsAsync(int topN, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
