using erp.Data;
using erp.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Sales;

public class SaleDao : ISaleDao
{
    private readonly ApplicationDbContext _context;

    public SaleDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        return await _context.Sales.FindAsync(id);
    }

    public async Task<Sale?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Sale>> GetAllAsync()
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<Sale> CreateAsync(Sale sale)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();
        return sale;
    }

    public async Task<Sale> UpdateAsync(Sale sale)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync();
        return sale;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sale = await _context.Sales.FindAsync(id);
        if (sale == null) return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetNextSaleNumberAsync(int tenantId)
    {
        var lastSaleNumber = await _context.Sales
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.SaleNumber)
            .Select(s => s.SaleNumber)
            .FirstOrDefaultAsync();

        if (lastSaleNumber == null) return "000001";

        if (int.TryParse(lastSaleNumber, out var currentNumber))
        {
            return (currentNumber + 1).ToString("D6");
        }

        return "000001";
    }
}
