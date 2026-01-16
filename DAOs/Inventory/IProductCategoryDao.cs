using erp.Models.Inventory;

namespace erp.DAOs.Inventory;

public interface IProductCategoryDao
{
    Task<List<ProductCategory>> GetAllAsync();
    Task<ProductCategory?> GetByIdAsync(int id);
    Task<ProductCategory?> GetByIdWithRelationsAsync(int id);
    Task<ProductCategory> CreateAsync(ProductCategory category);
    Task<ProductCategory> UpdateAsync(ProductCategory category);
    Task<bool> DeleteAsync(int id);
}
