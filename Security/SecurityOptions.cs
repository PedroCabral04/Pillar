namespace erp.Security;

/// <summary>
/// Security configuration options for the application.
/// Settings are loaded from appsettings.json under the "Security" section.
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>
    /// Default password for the admin user (only used during initial seed).
    /// WARNING: In production, this should be set via environment variables and changed immediately after first login.
    /// </summary>
    public string DefaultAdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// Allow demo login button on the login page. Should be false in production.
    /// </summary>
    public bool AllowDemoLogin { get; set; } = false;

    /// <summary>
    /// Require API key for /api endpoints. When true, all API requests must include X-Api-Key header.
    /// </summary>
    public bool RequireApiKey { get; set; } = false;

    /// <summary>
    /// The API key to validate against when RequireApiKey is true.
    /// Should be set via environment variables in production.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
