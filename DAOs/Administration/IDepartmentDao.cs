using erp.Models.Identity;

namespace erp.DAOs.Administration;

public interface IDepartmentDao
{
    Task<Department?> GetByIdAsync(int id);
    Task<Department?> GetByIdWithRelationsAsync(int id);
    Task<List<Department>> GetAllAsync(bool activeOnly = true);
    Task<Department?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<Department> CreateAsync(Department department);
    Task<Department> UpdateAsync(Department department);
    Task DeleteAsync(int id);
    Task<bool> HasEmployeesAsync(int id);
    Task<bool> HasSubDepartmentsAsync(int id);
}
