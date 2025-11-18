using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace erp.Services.Tenancy;

public class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantConnectionResolver> _logger;
    private readonly string _defaultConnectionString;

    private const string DefaultFallbackConnection = "Host=localhost;Database=erp;Username=postgres;Password=123";

    public TenantConnectionResolver(
        ITenantContextAccessor tenantContextAccessor,
        IConfiguration configuration,
        ILogger<TenantConnectionResolver> logger)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
        _defaultConnectionString = configuration.GetConnectionString("DefaultConnection") ?? DefaultFallbackConnection;
    }

    public string GetCurrentConnectionString()
    {
        var tenantContext = _tenantContextAccessor.Current;

        if (tenantContext.IsResolved)
        {
            if (!string.IsNullOrWhiteSpace(tenantContext.ConnectionString))
            {
                return tenantContext.ConnectionString!;
            }

            _logger.LogWarning(
                "Tenant {TenantSlug} resolved without connection string. Falling back to default database.",
                tenantContext.Slug ?? tenantContext.TenantId?.ToString() ?? "unknown");
        }

        return _defaultConnectionString;
    }
}
