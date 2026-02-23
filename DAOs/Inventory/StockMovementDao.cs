using erp.Data;
using erp.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Inventory;

public class StockMovementDao : IStockMovementDao
{
    private readonly ApplicationDbContext _context;

    public StockMovementDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockMovement?> GetByIdAsync(int id)
    {
        return await _context.StockMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<StockMovement>> GetByProductAsync(int productId)
    {
        return await _context.StockMovements
            .AsNoTracking()
            .Include(m => m.Warehouse)
            .Include(m => m.CreatedByUser)
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync();
    }

    public async Task<List<StockMovement>> GetByProductPagedAsync(int productId, int page, int pageSize)
    {
        return await _context.StockMovements
            .AsNoTracking()
            .Include(m => m.Warehouse)
            .Include(m => m.CreatedByUser)
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<StockMovement> CreateAsync(StockMovement movement)
    {
        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();
        return movement;
    }

    public async Task<StockMovement> UpdateAsync(StockMovement movement)
    {
        _context.StockMovements.Update(movement);
        await _context.SaveChangesAsync();
        return movement;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var movement = await _context.StockMovements.FindAsync(id);
        if (movement == null) return false;

        _context.StockMovements.Remove(movement);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DateTime?> GetLastMovementDateAsync(int productId)
    {
        return await _context.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.MovementDate)
            .Select(m => (DateTime?)m.MovementDate)
            .FirstOrDefaultAsync();
    }
}
