using erp.Models.Identity;

namespace erp.DAOs.Administration;

public interface IPositionDao
{
    Task<Position?> GetByIdAsync(int id);
    Task<Position?> GetByIdWithRelationsAsync(int id);
    Task<List<Position>> GetAllAsync(bool activeOnly = true);
    Task<Position?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<Position> CreateAsync(Position position);
    Task<Position> UpdateAsync(Position position);
    Task DeleteAsync(int id);
    Task<bool> HasEmployeesAsync(int id);
}
