using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace erp.Services.Tenancy;

public class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantConnectionResolver> _logger;
    private readonly string _defaultConnectionString;
    private readonly string _templateConnectionString;
    private readonly IConfiguration _configuration;

    public TenantConnectionResolver(
        ITenantContextAccessor tenantContextAccessor,
        IConfiguration configuration,
        ILogger<TenantConnectionResolver> logger)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _configuration = configuration;
        _logger = logger;
        _defaultConnectionString = configuration.GetConnectionString("DefaultConnection") 
                                   ?? configuration["DbContextSettings:ConnectionString"] 
                                   ?? "";
        _templateConnectionString = configuration["MultiTenancy:Database:TemplateConnectionString"] ?? "";

        // SECURITY: Log warning se usando credenciais padrão em desenvolvimento
        if (!string.IsNullOrEmpty(_defaultConnectionString) && _defaultConnectionString.Contains("Password=123"))
        {
            _logger.LogWarning("Using default database password. This should be changed in production!");
        }
        if (!string.IsNullOrEmpty(_templateConnectionString) && _templateConnectionString.Contains("Password=123"))
        {
            _logger.LogWarning("Using default tenant database password. This should be changed in production!");
        }
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

        // SECURITY: Retorna string vazia em vez de senha hardcoded se não configurado
        // Deixe o DbContext tratar a ausência de conexão
        return _defaultConnectionString;
    }

    public string GetTemplateConnectionString()
    {
        return _templateConnectionString;
    }
}
