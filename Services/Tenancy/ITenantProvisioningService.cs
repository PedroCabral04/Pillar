using erp.Models.Tenancy;

namespace erp.Services.Tenancy;

public interface ITenantProvisioningService
{
    Task ProvisionAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
