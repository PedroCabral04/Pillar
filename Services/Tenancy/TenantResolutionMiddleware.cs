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
                if (context.Items.ContainsKey(TenantItemKey))
                    context.Items.Remove(TenantItemKey);

                // Se não conseguimos resolver o tenant e não é uma rota pública, retorna 403
                if (!IsPublicPath(context.Request.Path))
                {
                    _logger.LogWarning("Tenant não resolvido para o caminho: {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Tenant não identificado. Acesso negado.");
                    return; // Importante: não chama _next(context)
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resolver tenant para o caminho: {Path}", context.Request.Path);
            contextAccessor.Clear();

            // Se houver erro na resolução do tenant e não for rota pública, retorna 403
            if (!IsPublicPath(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Erro ao identificar tenant. Acesso negado.");
                return; // Importante: não chama _next(context)
            }
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        // Paths exatos públicos. Inclui "/" para permitir acesso inicial e redirecionamento de autenticação.
        var exactPublicPaths = new[]
        {
            "/",
            "/health",
            "/api/health",
            "/favicon.ico",
            "/login",
            "/forgot-password",
            "/reset-password"
        };

        if (exactPublicPaths.Any(publicPath => string.Equals(path, publicPath, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Prefixos públicos para recursos estáticos e endpoints de autenticação.
        var publicPrefixes = new[]
        {
            "/_framework",
            "/_blazor",
            "/css",
            "/js",
            "/lib",
            "/Onboarding",
            "/api/auth",
            "/login/",
            "/account/"
        };

        return publicPrefixes.Any(publicPath => path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase));
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
