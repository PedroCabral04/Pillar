using erp.Models.Identity;

namespace erp.Services.Authorization;

/// <summary>
/// Service interface for checking module-level permissions
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if a user has access to a specific module
    /// </summary>
    Task<bool> HasModuleAccessAsync(int userId, string moduleKey);
    
    /// <summary>
    /// Check if a user has access to a specific module (by claims principal)
    /// </summary>
    Task<bool> HasModuleAccessAsync(System.Security.Claims.ClaimsPrincipal user, string moduleKey);
    
    /// <summary>
    /// Get all modules a user has access to
    /// </summary>
    Task<IReadOnlyList<string>> GetUserModulesAsync(int userId);
    
    /// <summary>
    /// Get all modules a user has access to (by claims principal)
    /// </summary>
    Task<IReadOnlyList<string>> GetUserModulesAsync(System.Security.Claims.ClaimsPrincipal user);
    
    /// <summary>
    /// Get all module permissions in the system
    /// </summary>
    Task<IReadOnlyList<ModulePermission>> GetAllModulesAsync();
    
    /// <summary>
    /// Get modules assigned to a specific role
    /// </summary>
    Task<IReadOnlyList<ModulePermission>> GetRoleModulesAsync(int roleId);
    
    /// <summary>
    /// Assign modules to a role
    /// </summary>
    Task AssignModulesToRoleAsync(int roleId, IEnumerable<int> modulePermissionIds, int? grantedByUserId = null);
    
    /// <summary>
    /// Remove all module assignments from a role and reassign the provided ones
    /// </summary>
    Task UpdateRoleModulesAsync(int roleId, IEnumerable<int> modulePermissionIds, int? grantedByUserId = null);
}
