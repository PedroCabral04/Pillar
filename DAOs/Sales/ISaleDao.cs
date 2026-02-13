using erp.DTOs.Sales;
using erp.Models.Sales;

namespace erp.DAOs.Sales;

public interface ISaleDao
{
    Task<Sale?> GetByIdAsync(int id);
    Task<Sale?> GetByIdWithRelationsAsync(int id);
    Task<List<Sale>> GetAllAsync();
    Task<SaleDto?> GetDtoByIdAsync(int id);
    Task<(List<SaleDto> items, int total)> GetPagedAsync(int page, int pageSize, int? tenantId = null, CancellationToken ct = default);
    Task<List<SaleSummaryDto>> GetSummariesAsync(int? tenantId = null, CancellationToken ct = default);
    Task<Sale> CreateAsync(Sale sale);
    Task<Sale> UpdateAsync(Sale sale);
    Task<bool> DeleteAsync(int id);
    Task<string?> GetNextSaleNumberAsync(int tenantId);
}

public class SaleSummaryDto
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
