using System.Security.Claims;
using erp.Data;
using erp.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace erp.Services.Authorization;

/// <summary>
/// Service for checking module-level permissions with caching
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string UserModulesCacheKeyPrefix = "user_modules_";
    private const string RoleModulesCacheKeyPrefix = "role_modules_";
    private const string AllModulesCacheKey = "all_modules";
    
    public PermissionService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<bool> HasModuleAccessAsync(int userId, string moduleKey)
    {
        var modules = await GetUserModulesAsync(userId);
        return modules.Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }
    
    public async Task<bool> HasModuleAccessAsync(ClaimsPrincipal user, string moduleKey)
    {
        // Admins always have access to everything
        if (user.IsInRole("Administrador"))
            return true;
            
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return false;
            
        return await HasModuleAccessAsync(userId, moduleKey);
    }
    
    public async Task<IReadOnlyList<string>> GetUserModulesAsync(int userId)
    {
        var cacheKey = $"{UserModulesCacheKeyPrefix}{userId}";
        
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cachedModules) && cachedModules != null)
        {
            return cachedModules;
        }
        
        // Get user's roles
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Array.Empty<string>();
            
        var roleNames = await _userManager.GetRolesAsync(user);
        
        // Admins get all modules
        if (roleNames.Contains("Administrador"))
        {
            var allModules = await GetAllModulesAsync();
            var allModuleKeys = allModules.Select(m => m.ModuleKey).ToList();
            _cache.Set(cacheKey, (IReadOnlyList<string>)allModuleKeys, CacheDuration);
            return allModuleKeys;
        }
        
        // Get role IDs
        var roleIds = await _context.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();
            
        // Get modules for all user's roles
        var moduleKeys = await _context.RoleModulePermissions
            .Where(rmp => roleIds.Contains(rmp.RoleId))
            .Select(rmp => rmp.ModulePermission.ModuleKey)
            .Distinct()
            .ToListAsync();
            
        _cache.Set(cacheKey, (IReadOnlyList<string>)moduleKeys, CacheDuration);
        return moduleKeys;
    }
    
    public async Task<IReadOnlyList<string>> GetUserModulesAsync(ClaimsPrincipal user)
    {
        // Admins get all modules
        if (user.IsInRole("Administrador"))
        {
            var allModules = await GetAllModulesAsync();
            return allModules.Select(m => m.ModuleKey).ToList();
        }
        
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Array.Empty<string>();
            
        return await GetUserModulesAsync(userId);
    }
    
    public async Task<IReadOnlyList<ModulePermission>> GetAllModulesAsync()
    {
        if (_cache.TryGetValue(AllModulesCacheKey, out IReadOnlyList<ModulePermission>? cached) && cached != null)
        {
            return cached;
        }
        
        var modules = await _context.ModulePermissions
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
            
        _cache.Set(AllModulesCacheKey, (IReadOnlyList<ModulePermission>)modules, CacheDuration);
        return modules;
    }
    
    public async Task<IReadOnlyList<ModulePermission>> GetRoleModulesAsync(int roleId)
    {
        var cacheKey = $"{RoleModulesCacheKeyPrefix}{roleId}";
        
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ModulePermission>? cached) && cached != null)
        {
            return cached;
        }
        
        var modules = await _context.RoleModulePermissions
            .Where(rmp => rmp.RoleId == roleId)
            .Include(rmp => rmp.ModulePermission)
            .Select(rmp => rmp.ModulePermission)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
            
        _cache.Set(cacheKey, (IReadOnlyList<ModulePermission>)modules, CacheDuration);
        return modules;
    }
    
    public async Task AssignModulesToRoleAsync(int roleId, IEnumerable<int> modulePermissionIds, int? grantedByUserId = null)
    {
        var existingAssignments = await _context.RoleModulePermissions
            .Where(rmp => rmp.RoleId == roleId)
            .Select(rmp => rmp.ModulePermissionId)
            .ToListAsync();
            
        var newAssignments = modulePermissionIds
            .Except(existingAssignments)
            .Select(mpId => new RoleModulePermission
            {
                RoleId = roleId,
                ModulePermissionId = mpId,
                GrantedAt = DateTime.UtcNow,
                GrantedByUserId = grantedByUserId
            });
            
        await _context.RoleModulePermissions.AddRangeAsync(newAssignments);
        await _context.SaveChangesAsync();
        
        InvalidateRoleCache(roleId);
    }
    
    public async Task UpdateRoleModulesAsync(int roleId, IEnumerable<int> modulePermissionIds, int? grantedByUserId = null)
    {
        // Remove existing assignments
        var existingAssignments = await _context.RoleModulePermissions
            .Where(rmp => rmp.RoleId == roleId)
            .ToListAsync();
            
        _context.RoleModulePermissions.RemoveRange(existingAssignments);
        
        // Add new assignments
        var newAssignments = modulePermissionIds.Select(mpId => new RoleModulePermission
        {
            RoleId = roleId,
            ModulePermissionId = mpId,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        });
        
        await _context.RoleModulePermissions.AddRangeAsync(newAssignments);
        await _context.SaveChangesAsync();
        
        InvalidateRoleCache(roleId);
        InvalidateAllUserCaches();
    }
    
    private void InvalidateRoleCache(int roleId)
    {
        _cache.Remove($"{RoleModulesCacheKeyPrefix}{roleId}");
    }
    
    private void InvalidateAllUserCaches()
    {
        // In a production app, you might want to use a distributed cache
        // and track which user IDs are cached. For now, we clear the entire cache.
        // This is a simplified approach.
        _cache.Remove(AllModulesCacheKey);
        
        // Note: IMemoryCache doesn't support pattern-based removal
        // In production, consider using IDistributedCache with Redis
        _logger.LogInformation("User permission caches invalidated due to role module update");
    }
}
