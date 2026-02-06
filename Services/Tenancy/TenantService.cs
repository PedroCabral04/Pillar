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
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantBrandingProvider _brandingProvider;

    public TenantService(
        ApplicationDbContext db,
        TenantMapper mapper,
        ILogger<TenantService> logger,
        ITenantContextAccessor tenantContextAccessor,
        ITenantBrandingProvider brandingProvider)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
        _tenantContextAccessor = tenantContextAccessor;
        _brandingProvider = brandingProvider;
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
        tenant.Status = TenantStatus.Active;
        tenant.Memberships = new List<TenantMembership>();

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);

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

        // Handle branding update manually to ensure proper EF Core tracking
        if (dto.Branding is not null)
        {
            if (tenant.Branding is null)
            {
                // Create new branding if it doesn't exist
                tenant.Branding = _mapper.TenantBrandingDtoToEntity(dto.Branding);
            }
            else
            {
                // Update existing branding
                _mapper.UpdateBrandingFromDto(dto.Branding, tenant.Branding);
            }
        }
        else if (tenant.Branding is not null)
        {
            // Remove branding if DTO has null branding
            _db.Remove(tenant.Branding);
            tenant.Branding = null;
        }

        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // If updating the current user's tenant, refresh the context
        if (_tenantContextAccessor.Current.TenantId == tenant.Id)
        {
            _tenantContextAccessor.SetTenant(tenant);
            _brandingProvider.NotifyBrandingChanged();
        }

        return _mapper.TenantToTenantDto(tenant);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        // Soft delete: marca como arquivado em vez de remover
        tenant.Status = TenantStatus.Archived;
        tenant.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantName} (ID: {TenantId}) arquivado", tenant.Name, id);
    }

    public async Task<IEnumerable<TenantDto>> GetUserTenantsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Busca tenants onde o usuário tem membership ativa
        var tenants = await _db.TenantMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.RevokedAt == null)
            .Include(m => m.Tenant)
                .ThenInclude(t => t!.Branding)
            .Where(m => m.Tenant != null && m.Tenant.Status == TenantStatus.Active)
            .Select(m => m.Tenant!)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return _mapper.TenantsToTenantDtos(tenants);
    }

    public async Task<bool> SlugExistsAsync(string slug, int? ignoreTenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var normalizedSlug = slug.ToLowerInvariant();
        return await _db.Tenants.AnyAsync(t => t.Slug == normalizedSlug
            && t.Status != TenantStatus.Archived
            && (!ignoreTenantId.HasValue || t.Id != ignoreTenantId), cancellationToken);
    }

    public async Task<IEnumerable<TenantMemberDto>> GetMembersAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var members = await _db.TenantMemberships
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Include(m => m.User)
            .OrderBy(m => m.RevokedAt.HasValue)
            .ThenBy(m => m.User.FullName ?? m.User.UserName)
            .Select(m => new TenantMemberDto(
                m.UserId,
                m.User.UserName ?? string.Empty,
                m.User.FullName,
                m.User.Email,
                m.IsDefault,
                m.CreatedAt,
                m.RevokedAt))
            .ToListAsync(cancellationToken);

        return members;
    }

    public async Task<TenantMemberDto> AssignMemberAsync(int tenantId, int userId, string? assignedBy, CancellationToken cancellationToken = default)
    {
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);
        if (!tenantExists)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} nao encontrado.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Usuario {userId} nao encontrado.");

        var membership = await _db.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);

        if (membership is null)
        {
            membership = new TenantMembership
            {
                TenantId = tenantId,
                UserId = userId,
                AssignedBy = assignedBy,
                CreatedAt = DateTime.UtcNow,
                IsDefault = false
            };

            _db.TenantMemberships.Add(membership);
        }
        else
        {
            membership.RevokedAt = null;
            membership.AssignedBy = assignedBy;
        }

        if (user.TenantId != tenantId)
        {
            user.TenantId = tenantId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new TenantMemberDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            user.Email,
            membership.IsDefault,
            membership.CreatedAt,
            membership.RevokedAt);
    }

    public async Task RevokeMemberAsync(int tenantId, int userId, CancellationToken cancellationToken = default)
    {
        var membership = await _db.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);

        if (membership is null)
        {
            return;
        }

        membership.RevokedAt = DateTime.UtcNow;
        membership.IsDefault = false;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
