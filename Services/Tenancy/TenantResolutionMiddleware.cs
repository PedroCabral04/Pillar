using System.Security.Claims;
using erp.Models.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace erp.Services.Tenancy;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public const string TenantItemKey = "pillar-tenant";

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, ITenantContextAccessor contextAccessor)
    {
        try
        {
            var tenant = await resolver.ResolveAsync(context, context.RequestAborted);
            if (tenant is not null)
            {
                StampTenantContext(context, contextAccessor, tenant);
            }
            else
            {
                contextAccessor.Clear();
                context.Items.Remove(TenantItemKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resolver tenant");
            contextAccessor.Clear();
        }

        await _next(context);
    }

    private static void StampTenantContext(HttpContext context, ITenantContextAccessor accessor, Tenant tenant)
    {
        accessor.SetTenant(tenant);
        context.Items[TenantResolutionMiddleware.TenantItemKey] = tenant;

        if (context.User.Identity is ClaimsIdentity identity && identity.IsAuthenticated)
        {
            if (!identity.HasClaim(c => c.Type == TenantClaimTypes.TenantId))
            {
                identity.AddClaim(new Claim(TenantClaimTypes.TenantId, tenant.Id.ToString()));
            }

            if (!identity.HasClaim(c => c.Type == TenantClaimTypes.TenantSlug))
            {
                identity.AddClaim(new Claim(TenantClaimTypes.TenantSlug, tenant.Slug));
            }
        }
    }
}
