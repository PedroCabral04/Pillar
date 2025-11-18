namespace erp.Services.Tenancy;

public class TenantDatabaseOptions
{
    /// <summary>
    /// Default connection string used as a template when no tenant-specific connection string is provided.
    /// Supports placeholders {SLUG} and {DB}.
    /// </summary>
    public string? TemplateConnectionString { get; set; }

    /// <summary>
    /// Name of the administrative database used when issuing CREATE DATABASE commands (defaults to "postgres").
    /// </summary>
    public string AdminDatabase { get; set; } = "postgres";

    /// <summary>
    /// Prefix applied to database names generated from tenant slugs.
    /// </summary>
    public string DatabasePrefix { get; set; } = "pillar_";

    /// <summary>
    /// Whether provisioning services should automatically mark tenants as Active after migrations succeed.
    /// </summary>
    public bool AutoActivateTenants { get; set; } = true;
}
