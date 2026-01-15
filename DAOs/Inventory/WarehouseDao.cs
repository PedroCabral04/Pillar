using erp.Data;
using erp.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Inventory;

public class WarehouseDao : IWarehouseDao
{
    private readonly ApplicationDbContext _context;

    public WarehouseDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Warehouse>> GetAllAsync()
    {
        return await _context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<Warehouse?> GetByIdAsync(int id)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Warehouse?> GetByCodeAsync(string code)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Code == code);
    }

    public async Task<Warehouse> CreateAsync(Warehouse warehouse)
    {
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();
        return warehouse;
    }

    public async Task<Warehouse> UpdateAsync(Warehouse warehouse)
    {
        _context.Warehouses.Update(warehouse);
        await _context.SaveChangesAsync();
        return warehouse;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null) return false;

        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync();
        return true;
    }
}
