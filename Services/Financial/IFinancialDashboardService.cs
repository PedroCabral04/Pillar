using erp.DTOs.Financial;

namespace erp.Services.Financial;

public interface IFinancialDashboardService
{
    Task<FinancialDashboardDto> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null);
}
