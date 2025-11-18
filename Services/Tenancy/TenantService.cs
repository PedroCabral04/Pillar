using erp.Data;
using erp.DTOs.Tenancy;
using erp.Mappings;
using erp.Models.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Tenancy;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _db;
    private readonly TenantMapper _mapper;
    private readonly ILogger<TenantService> _logger;
    private readonly ITenantProvisioningService _provisioningService;

    public TenantService(ApplicationDbContext db, TenantMapper mapper, ILogger<TenantService> logger, ITenantProvisioningService provisioningService)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
        _provisioningService = provisioningService;
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Branding)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return _mapper.TenantsToTenantDtos(tenants);
    }

    public async Task<TenantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return tenant is null ? null : _mapper.TenantToTenantDto(tenant);
    }

    public async Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.ToLowerInvariant();
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Slug == normalizedSlug, cancellationToken);

        return tenant is null ? null : _mapper.TenantToTenantDto(tenant);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var slugExists = await SlugExistsAsync(dto.Slug, null, cancellationToken);
        if (slugExists)
        {
            throw new InvalidOperationException($"Tenant slug '{dto.Slug}' já está em uso.");
        }

        var tenant = _mapper.CreateTenantDtoToTenant(dto);
        tenant.Slug = dto.Slug.ToLowerInvariant();
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.Status = TenantStatus.Provisioning;
        tenant.Memberships = new List<TenantMembership>();
        tenant.DatabaseName = dto.DatabaseName ?? tenant.DatabaseName;
        tenant.ConnectionString = dto.ConnectionString ?? tenant.ConnectionString;

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);

        if (dto.ProvisionDatabase)
        {
            await _provisioningService.ProvisionAsync(tenant, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Tenant {TenantName} criado pelo usuário {UserId}", tenant.Name, userId);

        return _mapper.TenantToTenantDto(tenant);
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
        {
            throw new KeyNotFoundException($"Tenant {id} não encontrado.");
        }

        _mapper.UpdateTenantFromDto(dto, tenant);
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.DatabaseName = dto.DatabaseName ?? tenant.DatabaseName;
        tenant.ConnectionString = dto.ConnectionString ?? tenant.ConnectionString;

        if (dto.ProvisionDatabase)
        {
            await _provisioningService.ProvisionAsync(tenant, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.TenantToTenantDto(tenant);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, int? ignoreTenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var normalizedSlug = slug.ToLowerInvariant();
        return await _db.Tenants.AnyAsync(t => t.Slug == normalizedSlug && (!ignoreTenantId.HasValue || t.Id != ignoreTenantId), cancellationToken);
    }

    public async Task ProvisionDatabaseAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException($"Tenant {id} não encontrado.");
        }

        await _provisioningService.ProvisionAsync(tenant, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
