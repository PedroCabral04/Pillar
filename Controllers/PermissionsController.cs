using erp.Data;
using erp.DTOs.Permissions;
using erp.Models.Identity;
using erp.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Controller for managing module permissions and role assignments
/// </summary>
[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "Administrador")]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;
    
    public PermissionsController(
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<PermissionsController> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all available modules in the system
    /// </summary>
    [HttpGet("modules")]
    public async Task<ActionResult<List<ModulePermissionDto>>> GetAllModules()
    {
        var modules = await _context.ModulePermissions
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new ModulePermissionDto
            {
                Id = m.Id,
                ModuleKey = m.ModuleKey,
                DisplayName = m.DisplayName,
                Description = m.Description,
                Icon = m.Icon,
                DisplayOrder = m.DisplayOrder,
                IsActive = m.IsActive
            })
            .ToListAsync();
            
        return Ok(modules);
    }
    
    /// <summary>
    /// Get all roles with their assigned modules
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<List<RolePermissionsDto>>> GetAllRolesWithPermissions()
    {
        var roles = await _context.Set<ApplicationRole>()
            .Include(r => r.ModulePermissions)
                .ThenInclude(rmp => rmp.ModulePermission)
            .OrderBy(r => r.Name)
            .Select(r => new RolePermissionsDto
            {
                RoleId = r.Id,
                RoleName = r.Name!,
                Abbreviation = r.Abbreviation,
                Description = r.Description,
                Icon = r.Icon,
                Modules = r.ModulePermissions
                    .Where(rmp => rmp.ModulePermission.IsActive)
                    .OrderBy(rmp => rmp.ModulePermission.DisplayOrder)
                    .Select(rmp => new ModulePermissionDto
                    {
                        Id = rmp.ModulePermission.Id,
                        ModuleKey = rmp.ModulePermission.ModuleKey,
                        DisplayName = rmp.ModulePermission.DisplayName,
                        Description = rmp.ModulePermission.Description,
                        Icon = rmp.ModulePermission.Icon,
                        DisplayOrder = rmp.ModulePermission.DisplayOrder,
                        IsActive = rmp.ModulePermission.IsActive
                    })
                    .ToList()
            })
            .ToListAsync();
            
        return Ok(roles);
    }
    
    /// <summary>
    /// Get a specific role with its assigned modules
    /// </summary>
    [HttpGet("roles/{roleId}")]
    public async Task<ActionResult<RolePermissionsDto>> GetRolePermissions(int roleId)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.ModulePermissions)
                .ThenInclude(rmp => rmp.ModulePermission)
            .FirstOrDefaultAsync(r => r.Id == roleId);
            
        if (role == null)
            return NotFound("Role not found");
            
        var dto = new RolePermissionsDto
        {
            RoleId = role.Id,
            RoleName = role.Name!,
            Abbreviation = role.Abbreviation,
            Description = role.Description,
            Icon = role.Icon,
            Modules = role.ModulePermissions
                .Where(rmp => rmp.ModulePermission.IsActive)
                .OrderBy(rmp => rmp.ModulePermission.DisplayOrder)
                .Select(rmp => new ModulePermissionDto
                {
                    Id = rmp.ModulePermission.Id,
                    ModuleKey = rmp.ModulePermission.ModuleKey,
                    DisplayName = rmp.ModulePermission.DisplayName,
                    Description = rmp.ModulePermission.Description,
                    Icon = rmp.ModulePermission.Icon,
                    DisplayOrder = rmp.ModulePermission.DisplayOrder,
                    IsActive = rmp.ModulePermission.IsActive
                })
                .ToList()
        };
        
        return Ok(dto);
    }
    
    /// <summary>
    /// Update modules assigned to a role
    /// </summary>
    [HttpPut("roles/{roleId}/modules")]
    public async Task<IActionResult> UpdateRoleModules(int roleId, [FromBody] UpdateRoleModulesDto dto)
    {
        if (roleId != dto.RoleId)
            return BadRequest("Role ID mismatch");
            
        var role = await _context.Set<ApplicationRole>().FindAsync(roleId);
        if (role == null)
            return NotFound("Role not found");
            
        // Prevent removing all modules from Administrador role
        if (role.Name == "Administrador" && dto.ModulePermissionIds.Count == 0)
        {
            return BadRequest("Cannot remove all modules from Administrador role");
        }
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? grantedByUserId = int.TryParse(userId, out var id) ? id : null;
        
        try
        {
            await _permissionService.UpdateRoleModulesAsync(roleId, dto.ModulePermissionIds, grantedByUserId);
            
            _logger.LogInformation(
                "User {UserId} updated modules for role {RoleId} ({RoleName}): {Modules}",
                userId, roleId, role.Name, string.Join(", ", dto.ModulePermissionIds));
                
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role modules for role {RoleId}", roleId);
            return StatusCode(500, "Error updating role modules");
        }
    }
    
    /// <summary>
    /// Get current user's accessible modules
    /// </summary>
    [HttpGet("my-modules")]
    [Authorize]
    public async Task<ActionResult<UserModulesDto>> GetMyModules()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
            return Unauthorized();
            
        var moduleKeys = await _permissionService.GetUserModulesAsync(id);
        var allModules = await _permissionService.GetAllModulesAsync();
        
        var dto = new UserModulesDto
        {
            UserId = id,
            UserName = User.Identity?.Name,
            ModuleKeys = moduleKeys.ToList(),
            Modules = allModules
                .Where(m => moduleKeys.Contains(m.ModuleKey))
                .Select(m => new ModulePermissionDto
                {
                    Id = m.Id,
                    ModuleKey = m.ModuleKey,
                    DisplayName = m.DisplayName,
                    Description = m.Description,
                    Icon = m.Icon,
                    DisplayOrder = m.DisplayOrder,
                    IsActive = m.IsActive
                })
                .ToList()
        };
        
        return Ok(dto);
    }
    
    /// <summary>
    /// Check if current user has access to a specific module
    /// </summary>
    [HttpGet("check/{moduleKey}")]
    [Authorize]
    public async Task<ActionResult<bool>> CheckModuleAccess(string moduleKey)
    {
        var hasAccess = await _permissionService.HasModuleAccessAsync(User, moduleKey);
        return Ok(hasAccess);
    }
}
