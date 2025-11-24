using erp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace erp.Services.Tenancy;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string _migrationsAssembly;

    public TenantDbContextFactory(IHttpContextAccessor? httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _migrationsAssembly = typeof(ApplicationDbContext).Assembly.FullName ?? typeof(ApplicationDbContext).Assembly.GetName().Name!;
    }

    public ApplicationDbContext CreateDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required to create tenant DbContext", nameof(connectionString));
        }

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(_migrationsAssembly));
        return new ApplicationDbContext(builder.Options, _httpContextAccessor);
    }
}
