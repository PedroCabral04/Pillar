using erp.DTOs.Financial;

namespace erp.Services.Financial;

public interface IVendorPerformanceService
{
    Task<VendorPerformanceDto?> GetByUserAndPeriodAsync(int userId, int year, int month);
    Task<List<VendorPerformanceDto>> GetByTenantAndPeriodAsync(int tenantId, int year, int month);
    Task<List<VendorPerformanceDto>> GetTopPerformersAsync(int tenantId, int year, int month, int topN);
    Task<VendorPerformanceDto> CalculatePerformanceAsync(int userId, int year, int month);
    Task RecalculatePeriodAsync(int tenantId, int year, int month);
}
