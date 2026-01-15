using erp.DTOs.User;

namespace erp.Services.Administration;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync(bool activeOnly = true);
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
    Task UpdateAsync(int id, UpdateDepartmentDto dto);
    Task DeleteAsync(int id);
}
