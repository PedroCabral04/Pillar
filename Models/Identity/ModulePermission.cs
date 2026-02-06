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

    /// <summary>
    /// Action-level permissions available inside this module.
    /// </summary>
    public ICollection<ModuleActionPermission> Actions { get; set; } = new List<ModuleActionPermission>();
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
/// Represents a fine-grained action inside a module (e.g., view_values, export, edit).
/// </summary>
public class ModuleActionPermission
{
    public int Id { get; set; }

    /// <summary>
    /// Module to which this action belongs.
    /// </summary>
    public int ModulePermissionId { get; set; }
    public ModulePermission ModulePermission { get; set; } = null!;

    /// <summary>
    /// Stable key used by backend and frontend checks.
    /// </summary>
    public required string ActionKey { get; set; }

    /// <summary>
    /// Human friendly label for admin UI.
    /// </summary>
    public required string DisplayName { get; set; }

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RoleModuleActionPermission> RolePermissions { get; set; } = new List<RoleModuleActionPermission>();
}

/// <summary>
/// Junction table linking roles to action-level permissions.
/// </summary>
public class RoleModuleActionPermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;

    public int ModuleActionPermissionId { get; set; }
    public ModuleActionPermission ModuleActionPermission { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
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

/// <summary>
/// Known action keys for module-level fine-grained authorization.
/// </summary>
public static class ModuleActionKeys
{
    public static class Common
    {
        public const string ViewPage = "view_page";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
        public const string Export = "export";
    }

    public static class Sales
    {
        public const string ViewHistory = "view_history";
        public const string ViewValues = "view_values";
        public const string Finalize = "finalize";
        public const string Cancel = "cancel";
        public const string ManageCustomers = "manage_customers";
        public const string ExportPdf = "export_pdf";
    }

    public static class ServiceOrder
    {
        public const string Finalize = "finalize";
        public const string Reopen = "reopen";
        public const string Cancel = "cancel";
        public const string ViewCosts = "view_costs";
    }

    public static class Inventory
    {
        public const string AdjustStock = "adjust_stock";
        public const string ViewCosts = "view_costs";
        public const string ManageCategories = "manage_categories";
    }

    public static class Financial
    {
        public const string ViewBalances = "view_balances";
        public const string Approve = "approve";
        public const string ManageSuppliers = "manage_suppliers";
        public const string ManageCostCenters = "manage_cost_centers";
    }

    public static class HR
    {
        public const string ManagePayroll = "manage_payroll";
        public const string ViewSalaryData = "view_salary_data";
        public const string ManageAttendance = "manage_attendance";
    }

    public static class Assets
    {
        public const string Transfer = "transfer";
        public const string Depreciation = "depreciation";
    }

    public static class Kanban
    {
        public const string ManageBoards = "manage_boards";
        public const string ManageMembers = "manage_members";
    }

    public static class Reports
    {
        public const string Sales = "sales";
        public const string Financial = "financial";
        public const string Inventory = "inventory";
        public const string HR = "hr";
    }

    public static class Admin
    {
        public const string ManageRoles = "manage_roles";
        public const string ManageTenants = "manage_tenants";
        public const string ViewAudit = "view_audit";
        public const string ViewLgpd = "view_lgpd";
    }

    public static class Dashboard
    {
        public const string ViewWidgets = "view_widgets";
        public const string ViewSensitiveWidgets = "view_sensitive_widgets";
    }
}
