using erp.Models.Sales;

namespace erp.DAOs.Sales;

public interface ISaleDao
{
    Task<Sale?> GetByIdAsync(int id);
    Task<Sale?> GetByIdWithRelationsAsync(int id);
    Task<List<Sale>> GetAllAsync();
    Task<Sale> CreateAsync(Sale sale);
    Task<Sale> UpdateAsync(Sale sale);
    Task<bool> DeleteAsync(int id);
    Task<string?> GetNextSaleNumberAsync(int tenantId);
}
