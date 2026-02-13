using erp.Data;
using erp.DTOs.Sales;
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

    public async Task<SaleDto?> GetDtoByIdAsync(int id)
    {
        return await _context.Sales
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SaleDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer != null ? s.Customer.Name : null,
                UserId = s.UserId,
                UserName = s.User != null ? s.User.Name : string.Empty,
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                DiscountAmount = s.DiscountAmount,
                NetAmount = s.NetAmount,
                Status = s.Status,
                PaymentMethod = s.PaymentMethod,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                Items = s.Items.Select(si => new SaleItemDto
                {
                    Id = si.Id,
                    ProductId = si.ProductId,
                    ProductName = si.Product != null ? si.Product.Name : string.Empty,
                    ProductSku = si.Product != null ? si.Product.Sku : string.Empty,
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    CostPrice = si.CostPrice,
                    Discount = si.Discount,
                    Total = si.Total
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(List<SaleDto> items, int total)> GetPagedAsync(int page, int pageSize, int? tenantId = null, CancellationToken ct = default)
    {
        var query = _context.Sales.AsNoTracking();

        if (tenantId.HasValue)
        {
            query = query.Where(s => s.TenantId == tenantId.Value);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SaleDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer != null ? s.Customer.Name : null,
                UserId = s.UserId,
                UserName = s.User != null ? s.User.Name : string.Empty,
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                DiscountAmount = s.DiscountAmount,
                NetAmount = s.NetAmount,
                Status = s.Status,
                PaymentMethod = s.PaymentMethod,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                Items = s.Items.Select(si => new SaleItemDto
                {
                    Id = si.Id,
                    ProductId = si.ProductId,
                    ProductName = si.Product != null ? si.Product.Name : string.Empty,
                    ProductSku = si.Product != null ? si.Product.Sku : string.Empty,
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    CostPrice = si.CostPrice,
                    Discount = si.Discount,
                    Total = si.Total
                }).ToList()
            })
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<SaleSummaryDto>> GetSummariesAsync(int? tenantId = null, CancellationToken ct = default)
    {
        var query = _context.Sales.AsNoTracking();

        if (tenantId.HasValue)
        {
            query = query.Where(s => s.TenantId == tenantId.Value);
        }

        return await query
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleSummaryDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                CustomerName = s.Customer != null ? s.Customer.Name : null,
                SaleDate = s.SaleDate,
                NetAmount = s.NetAmount,
                Status = s.Status
            })
            .ToListAsync(ct);
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
