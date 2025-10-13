using erp.DTOs.Sales;

namespace erp.Services.Sales;

public interface ISalesService
{
    Task<SaleDto> CreateAsync(CreateSaleDto dto, int userId);
    Task<SaleDto?> GetByIdAsync(int id);
    Task<(List<SaleDto> items, int total)> SearchAsync(
        string? search, 
        string? status, 
        DateTime? startDate, 
        DateTime? endDate, 
        int? customerId,
        int page, 
        int pageSize);
    Task<SaleDto> UpdateAsync(int id, UpdateSaleDto dto);
    Task<bool> CancelAsync(int id);
    Task<SaleDto> FinalizeAsync(int id);
    Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate);
    Task<List<(string productName, decimal quantity)>> GetTopProductsAsync(int topN, DateTime startDate, DateTime endDate);
}
