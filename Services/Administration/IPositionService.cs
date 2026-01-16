using erp.DTOs.User;

namespace erp.Services.Administration;

public interface IPositionService
{
    Task<List<PositionDto>> GetAllAsync(bool activeOnly = true);
    Task<PositionDto?> GetByIdAsync(int id);
    Task<PositionDto> CreateAsync(CreatePositionDto dto);
    Task UpdateAsync(int id, UpdatePositionDto dto);
    Task DeleteAsync(int id);
}
