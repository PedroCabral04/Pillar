using Microsoft.AspNetCore.Identity;

namespace erp.Models.Identity;

public class ApplicationRole : IdentityRole<int>
{
	public string? Abbreviation { get; set; }
    public int? TenantId { get; set; }
    
    /// <summary>
    /// Description of the role's purpose
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Icon for display in UI (MudBlazor icon class)
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Module permissions assigned to this role
    /// </summary>
    public ICollection<RoleModulePermission> ModulePermissions { get; set; } = new List<RoleModulePermission>();

    /// <summary>
    /// Action-level permissions assigned to this role.
    /// </summary>
    public ICollection<RoleModuleActionPermission> ModuleActionPermissions { get; set; } = new List<RoleModuleActionPermission>();
}
