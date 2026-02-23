using erp.Data;
using erp.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Sales;

public class CustomerDao : ICustomerDao
{
    private readonly ApplicationDbContext _context;

    public CustomerDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<Customer?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.CreatedByUser)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByDocumentAsync(string document)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Document == document);
    }

    public async Task<bool> DocumentExistsAsync(string document, int? excludeId = null)
    {
        var query = _context.Customers.Where(c => c.Document == document);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }
}
