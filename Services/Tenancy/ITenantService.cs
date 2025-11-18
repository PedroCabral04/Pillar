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
    Task ProvisionDatabaseAsync(int id, CancellationToken cancellationToken = default);
    Task<TenantConnectionInfoDto?> GetConnectionInfoAsync(int id, CancellationToken cancellationToken = default);
}
