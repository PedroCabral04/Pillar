using erp.Data;

namespace erp.Services.Tenancy;

public interface ITenantDbContextFactory
{
    ApplicationDbContext CreateDbContext(string connectionString);
}
