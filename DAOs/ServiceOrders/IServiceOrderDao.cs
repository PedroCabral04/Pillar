using erp.Models.ServiceOrders;

namespace erp.DAOs.ServiceOrders;

public interface IServiceOrderDao
{
    Task<ServiceOrder?> GetByIdAsync(int id);
    Task<ServiceOrder?> GetByIdWithRelationsAsync(int id);
    Task<List<ServiceOrder>> GetAllAsync();
    Task<ServiceOrder> CreateAsync(ServiceOrder serviceOrder);
    Task<ServiceOrder> UpdateAsync(ServiceOrder serviceOrder);
    Task<bool> DeleteAsync(int id);
    Task<string?> GetNextOrderNumberAsync(int tenantId);
}
