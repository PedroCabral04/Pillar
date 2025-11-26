namespace erp.Models.Dashboard;

/// <summary>
/// Stores role-based access configuration for dashboard widgets.
/// Admins can configure which roles have access to which widgets.
/// </summary>
public class WidgetRoleConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// The provider key (e.g., "sales", "finance", "inventory").
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The widget key within the provider (e.g., "sales-by-month", "cashflow").
    /// </summary>
    public string WidgetKey { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON array of role names that can access this widget.
    /// If null or empty, the widget uses its provider's default roles.
    /// An empty array "[]" means all authenticated users can access.
    /// </summary>
    public string? RolesJson { get; set; }
    
    /// <summary>
    /// When this configuration was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User ID who last modified this configuration.
    /// </summary>
    public int? ModifiedByUserId { get; set; }
    
    // Navigation property
    public virtual Models.Identity.ApplicationUser? ModifiedByUser { get; set; }
}
