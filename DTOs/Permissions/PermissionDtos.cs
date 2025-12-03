namespace erp.DTOs.Permissions;

/// <summary>
/// DTO for module permission information
/// </summary>
public class ModulePermissionDto
{
    public int Id { get; set; }
    public required string ModuleKey { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for role with its module permissions
/// </summary>
public class RolePermissionsDto
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public string? Abbreviation { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public List<ModulePermissionDto> Modules { get; set; } = new();
}

/// <summary>
/// DTO for updating role module assignments
/// </summary>
public class UpdateRoleModulesDto
{
    public int RoleId { get; set; }
    public List<int> ModulePermissionIds { get; set; } = new();
}

/// <summary>
/// DTO for user's accessible modules
/// </summary>
public class UserModulesDto
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public List<string> ModuleKeys { get; set; } = new();
    public List<ModulePermissionDto> Modules { get; set; } = new();
}
