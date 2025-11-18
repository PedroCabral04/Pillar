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
        var slugCandidate = ExtractSlugFromHeader(httpContext) ?? ExtractSlugFromHost(httpContext);

        if (!string.IsNullOrWhiteSpace(slugCandidate))
        {
            var tenant = await FindTenantBySlugAsync(slugCandidate, cancellationToken);
            if (tenant is not null)
            {
                return tenant;
            }
        }

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var tenantFromClaim = await ResolveFromUserAsync(httpContext.User, cancellationToken);
            if (tenantFromClaim is not null)
            {
                return tenantFromClaim;
            }
        }

        return null;
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

        return identityUser?.Tenant;
    }

    private async Task<Tenant?> FindTenantBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.ToLowerInvariant();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Tenants
            .AsNoTracking()
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Slug == normalized, cancellationToken);
    }

    private static string? ExtractSlugFromHeader(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant", out var slugHeader))
        {
            return slugHeader.FirstOrDefault()?.Trim().ToLowerInvariant();
        }

        return null;
    }

    private static string? ExtractSlugFromHost(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        // Ignore localhost or direct IPs
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || System.Net.IPAddress.TryParse(host, out _))
        {
            return null;
        }

        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3)
        {
            return null;
        }

        var candidate = segments[0];
        return string.IsNullOrWhiteSpace(candidate) ? null : candidate.ToLowerInvariant();
    }
}
