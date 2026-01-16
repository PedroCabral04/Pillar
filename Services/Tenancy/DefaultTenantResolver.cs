using System.Security.Claims;
using erp.Models.Identity;
using erp.Models.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Tenancy;

public class DefaultTenantResolver : ITenantResolver
{
    private readonly IDbContextFactory<erp.Data.ApplicationDbContext> _dbFactory;
    private readonly ILogger<DefaultTenantResolver> _logger;

    public DefaultTenantResolver(IDbContextFactory<erp.Data.ApplicationDbContext> dbFactory, ILogger<DefaultTenantResolver> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<Tenant?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        // 1. Try from Header (X-Tenant-Id or X-Tenant slug) - Primary for API calls
        var tenantIdStr = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (int.TryParse(tenantIdStr, out var tenantId))
        {
            var tenant = await FindTenantByIdAsync(tenantId, cancellationToken);
            if (tenant != null) return tenant;
        }

        var slugCandidate = ExtractSlugFromHeader(httpContext);
        if (!string.IsNullOrWhiteSpace(slugCandidate))
        {
            var tenant = await FindTenantBySlugAsync(slugCandidate, cancellationToken);
            if (tenant is not null) return tenant;
        }

        // 2. Try from User context (Claims/DB)
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var tenantFromClaim = await ResolveFromUserAsync(httpContext.User, cancellationToken);
            if (tenantFromClaim is not null) return tenantFromClaim;
        }

        return null;
    }

    private async Task<Tenant?> FindTenantByIdAsync(int id, CancellationToken cancellationToken)
    {
        try 
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var tenant = await db.Tenants
                .AsNoTracking()
                .Include(t => t.Branding)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (tenant != null && IsTenantAccessible(tenant)) return tenant;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar tenant por ID {Id}", id);
            return null;
        }
    }

    private async Task<Tenant?> ResolveFromUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        // Prefer tenant claim if already issued
        var tenantSlugClaim = user.FindFirst(TenantClaimTypes.TenantSlug)?.Value;
        if (!string.IsNullOrWhiteSpace(tenantSlugClaim))
        {
            var tenant = await FindTenantBySlugAsync(tenantSlugClaim, cancellationToken);
            if (tenant is not null)
            {
                return tenant;
            }
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var identityUser = await db.Set<ApplicationUser>()
            .AsNoTracking()
            .Include(u => u.Tenant)!
                .ThenInclude(t => t!.Branding)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        // Verificar se o tenant do usuário está acessível
        if (identityUser?.Tenant is not null && !IsTenantAccessible(identityUser.Tenant))
        {
            _logger.LogWarning("Usuário {UserId} pertence ao tenant '{Slug}' com status {Status} - acesso negado",
                userId, identityUser.Tenant.Slug, identityUser.Tenant.Status);
            return null;
        }

        return identityUser?.Tenant;
    }

    private async Task<Tenant?> FindTenantBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.ToLowerInvariant();
        
        try 
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            var tenant = await db.Tenants
                .AsNoTracking()
                .Include(t => t.Branding)
                .FirstOrDefaultAsync(t => t.Slug == normalized, cancellationToken);

            if (tenant == null)
            {
                return null;
            }

            // Verificar se o tenant está em um status que permite acesso
            if (!IsTenantAccessible(tenant))
            {
                _logger.LogWarning("Tenant '{Slug}' encontrado mas com status {Status} - acesso negado", 
                    slug, tenant.Status);
                return null;
            }

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar tenant por slug {Slug}", slug);
            return null;
        }
    }

    /// <summary>
    /// Verifica se o tenant está em um status que permite acesso ao sistema.
    /// Apenas tenants Active e Provisioning (para setup inicial) podem acessar.
    /// </summary>
    private static bool IsTenantAccessible(Tenant tenant)
    {
        return tenant.Status is TenantStatus.Active or TenantStatus.Provisioning;
    }

    private static string? ExtractSlugFromHeader(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant", out var slugHeader))
        {
            return slugHeader.FirstOrDefault()?.Trim().ToLowerInvariant();
        }

        return null;
    }
}
