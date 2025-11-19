namespace erp.Services.Tenancy;

public interface ITenantConnectionResolver
{
    string GetCurrentConnectionString();
}
