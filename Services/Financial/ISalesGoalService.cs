using erp.DTOs.Financial;

namespace erp.Services.Financial;

public interface ISalesGoalService
{
    Task<List<SalesGoalDto>> GetByUserIdAsync(int userId);
    Task<SalesGoalDto?> GetByUserAndPeriodAsync(int userId, int year, int month);
    Task<SalesGoalDto> CreateAsync(CreateSalesGoalDto dto);
    Task<SalesGoalDto> UpdateAsync(int id, UpdateSalesGoalDto dto);
    Task DeleteAsync(int id);
    Task<List<SalesGoalDto>> GetByTenantAndPeriodAsync(int tenantId, int year, int month);
}
