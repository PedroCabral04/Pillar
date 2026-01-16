using erp.Data;
using erp.Models.ServiceOrders;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.ServiceOrders;

public class ServiceOrderDao : IServiceOrderDao
{
    private readonly ApplicationDbContext _context;

    public ServiceOrderDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceOrder?> GetByIdAsync(int id)
    {
        return await _context.ServiceOrders.FindAsync(id);
    }

    public async Task<ServiceOrder?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.ServiceOrders
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<ServiceOrder>> GetAllAsync()
    {
        return await _context.ServiceOrders
            .Include(s => s.Customer)
            .Include(s => s.User)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceOrder> CreateAsync(ServiceOrder serviceOrder)
    {
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();
        return serviceOrder;
    }

    public async Task<ServiceOrder> UpdateAsync(ServiceOrder serviceOrder)
    {
        _context.ServiceOrders.Update(serviceOrder);
        await _context.SaveChangesAsync();
        return serviceOrder;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null) return false;

        _context.ServiceOrders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetNextOrderNumberAsync(int tenantId)
    {
        var lastOrderNumber = await _context.ServiceOrders
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.OrderNumber)
            .Select(s => s.OrderNumber)
            .FirstOrDefaultAsync();

        if (lastOrderNumber == null) return "000001";

        if (int.TryParse(lastOrderNumber, out var currentNumber))
        {
            return (currentNumber + 1).ToString("D6");
        }

        return "000001";
    }
}
