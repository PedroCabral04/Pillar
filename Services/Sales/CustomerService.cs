using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Sales;
using erp.Models.Sales;
using erp.Mappings;

namespace erp.Services.Sales;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly erp.Services.Tenancy.ITenantContextAccessor _tenantContextAccessor;
    private readonly SalesMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ApplicationDbContext context, 
        erp.Services.Tenancy.ITenantContextAccessor tenantContextAccessor,
        SalesMapper mapper,
        ILogger<CustomerService> logger)
    {
        _context = context;
        _tenantContextAccessor = tenantContextAccessor;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Check if document already exists
            if (await _context.Customers.AnyAsync(c => c.Document == dto.Document))
            {
                throw new InvalidOperationException($"Cliente com documento {dto.Document} já existe");
            }

            var customer = _mapper.ToEntity(dto);
            customer.CreatedAt = DateTime.UtcNow;
            
            // Explicitly set TenantId if available in context
            if (customer.TenantId == 0 && _tenantContextAccessor.Current.TenantId.HasValue)
            {
                customer.TenantId = _tenantContextAccessor.Current.TenantId.Value;
                _logger.LogInformation("Setting TenantId {TenantId} explicitly in CustomerService", customer.TenantId);
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Cliente {CustomerId} criado com sucesso. Documento: {Document}", 
                customer.Id, customer.Document);

            return _mapper.ToDto(customer);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao criar cliente com documento {Document}", dto.Document);
            throw;
        }
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        return customer == null ? null : _mapper.ToDto(customer);
    }

    public async Task<CustomerDto?> GetByDocumentAsync(string document)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Document == document);
        return customer == null ? null : _mapper.ToDto(customer);
    }

    public async Task<(List<CustomerDto> items, int total)> SearchAsync(
        string? search, 
        bool? isActive, 
        int page, 
        int pageSize)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => 
                c.Name.Contains(search) || 
                c.Document.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var total = await query.CountAsync();

        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = customers.Select(c => _mapper.ToDto(c)).ToList();
        return (dtos, total);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Cliente com ID {id} não encontrado");
            }

            _mapper.UpdateEntity(dto, customer);
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Cliente {CustomerId} atualizado com sucesso", customer.Id);

            return _mapper.ToDto(customer);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao atualizar cliente {CustomerId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return false;
            }

            // Soft delete
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Cliente {CustomerId} inativado com sucesso", customer.Id);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao inativar cliente {CustomerId}", id);
            throw;
        }
    }
}
