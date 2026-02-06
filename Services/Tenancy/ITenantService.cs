using erp.DTOs.Tenancy;

namespace erp.Services.Tenancy;

public interface ITenantService
{
    Task<IEnumerable<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(CreateTenantDto dto, int userId, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? ignoreTenantId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantMemberDto>> GetMembersAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<TenantMemberDto> AssignMemberAsync(int tenantId, int userId, string? assignedBy, CancellationToken cancellationToken = default);
    Task RevokeMemberAsync(int tenantId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna os tenants que o usuário tem acesso via TenantMemberships.
    /// </summary>
    Task<IEnumerable<TenantDto>> GetUserTenantsAsync(int userId, CancellationToken cancellationToken = default);
}
