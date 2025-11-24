using erp.Models.Tenancy;
using Microsoft.AspNetCore.Http;

namespace erp.Services.Tenancy;

public interface ITenantResolver
{
    Task<Tenant?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
