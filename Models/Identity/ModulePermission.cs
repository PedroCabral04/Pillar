namespace erp.Models.Identity;

/// <summary>
/// Represents a module that can be accessed in the system.
/// Each module corresponds to a section/feature of the ERP.
/// </summary>
public class ModulePermission
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique key for the module (e.g., "sales", "inventory", "financial")
    /// </summary>
    public required string ModuleKey { get; set; }
    
    /// <summary>
    /// Display name for the module (e.g., "Vendas", "Estoque", "Financeiro")
    /// </summary>
    public required string DisplayName { get; set; }
    
    /// <summary>
    /// Description of what the module provides
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Icon class for the module (MudBlazor icon)
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Order for display in UI
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Whether this module is active/available
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Navigation collection for role assignments
    /// </summary>
    public ICollection<RoleModulePermission> RolePermissions { get; set; } = new List<RoleModulePermission>();
}

/// <summary>
/// Junction table linking roles to module permissions.
/// Determines which roles have access to which modules.
/// </summary>
public class RoleModulePermission
{
    public int Id { get; set; }
    
    /// <summary>
    /// The role that has this permission
    /// </summary>
    public int RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;
    
    /// <summary>
    /// The module permission being granted
    /// </summary>
    public int ModulePermissionId { get; set; }
    public ModulePermission ModulePermission { get; set; } = null!;
    
    /// <summary>
    /// When this permission was granted
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User who granted this permission (for audit)
    /// </summary>
    public int? GrantedByUserId { get; set; }
    public ApplicationUser? GrantedByUser { get; set; }
}

/// <summary>
/// Static class containing all module keys for type-safe access.
/// Module keys must match route paths (e.g., "sales", "service-orders", "inventory").
/// IMPORTANT: When adding a new module key, also update PermissionService.AllModuleKeys.
/// </summary>
public static class ModuleKeys
{
    public const string Dashboard = "dashboard";
    public const string Sales = "sales";
    public const string ServiceOrder = "service-orders";
    public const string Inventory = "inventory";
    public const string Financial = "financial";
    public const string HR = "hr";
    public const string Assets = "assets";
    public const string Kanban = "kanban";
    public const string Reports = "reports";
    public const string Admin = "admin";
}
