namespace erp.Models.Dashboard;

/// <summary>
/// Persists user-specific dashboard layout configuration to the database.
/// </summary>
public class UserDashboardLayout
{
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the user who owns this layout.
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// JSON serialized layout configuration containing widget positions, sizes, visibility, etc.
    /// </summary>
    public string LayoutJson { get; set; } = "{}";
    
    /// <summary>
    /// Layout type: "grid", "list", "compact"
    /// </summary>
    public string LayoutType { get; set; } = "grid";
    
    /// <summary>
    /// Number of columns in grid layout.
    /// </summary>
    public int Columns { get; set; } = 3;
    
    /// <summary>
    /// When this layout was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this layout was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual Models.Identity.ApplicationUser? User { get; set; }
}
