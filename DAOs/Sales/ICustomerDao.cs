using erp.Models.Sales;

namespace erp.DAOs.Sales;

public interface ICustomerDao
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByIdWithRelationsAsync(int id);
    Task<Customer?> GetByDocumentAsync(string document);
    Task<bool> DocumentExistsAsync(string document, int? excludeId = null);
    Task<List<Customer>> GetAllAsync();
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
}
