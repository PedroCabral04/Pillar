using erp.Models.Inventory;

namespace erp.DAOs.Inventory;

public interface IWarehouseDao
{
    Task<List<Warehouse>> GetAllAsync();
    Task<Warehouse?> GetByIdAsync(int id);
    Task<Warehouse?> GetByCodeAsync(string code);
    Task<Warehouse> CreateAsync(Warehouse warehouse);
    Task<Warehouse> UpdateAsync(Warehouse warehouse);
    Task<bool> DeleteAsync(int id);
}
