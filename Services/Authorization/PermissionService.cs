using System.Security.Claims;
using erp.Data;
using erp.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace erp.Services.Authorization;

/// <summary>
/// Service for checking module-level permissions with caching.
/// Uses IDbContextFactory to avoid concurrency issues in Blazor Server.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string UserModulesCacheKeyPrefix = "user_modules_";
    private const string RoleModulesCacheKeyPrefix = "role_modules_";
    private const string RoleModuleActionsCacheKeyPrefix = "role_module_actions_";
    private const string AllModulesCacheKey = "all_modules";
    
    public PermissionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _contextFactory = contextFactory;
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
        
        // Admins get all modules - return hardcoded list to avoid DB dependency
        if (roleNames.Contains("Administrador"))
        {
            _cache.Set(cacheKey, AllModuleKeys, CacheDuration);
            return AllModuleKeys;
        }
        
        // Use factory to create a new DbContext instance
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Get role IDs - use Set<ApplicationRole>() to avoid the overridden Roles DbSet
        var roleIds = await context.Set<ApplicationRole>()
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();
            
        // Get modules for all user's roles
        var moduleKeys = await context.RoleModulePermissions
            .Where(rmp => roleIds.Contains(rmp.RoleId))
            .Select(rmp => rmp.ModulePermission.ModuleKey)
            .Distinct()
            .ToListAsync();
        
        _logger.LogInformation("[PermissionService] User {UserId} roles: [{Roles}], modules: [{Modules}]", 
            userId, string.Join(", ", roleNames), string.Join(", ", moduleKeys));
            
        _cache.Set(cacheKey, (IReadOnlyList<string>)moduleKeys, CacheDuration);
        return moduleKeys;
    }
    
    public async Task<IReadOnlyList<string>> GetUserModulesAsync(ClaimsPrincipal user)
    {
        // Admins get all modules - return hardcoded list to avoid DB dependency
        if (user.IsInRole("Administrador"))
        {
            return AllModuleKeys;
        }
        
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Array.Empty<string>();
            
        return await GetUserModulesAsync(userId);
    }
    
    /// <summary>
    /// All available module keys (for admins and fallback scenarios).
    /// IMPORTANT: Keep this array in sync with ModuleKeys class - add new keys here when adding modules.
    /// </summary>
    private static readonly IReadOnlyList<string> AllModuleKeys = new[]
    {
        ModuleKeys.Dashboard,
        ModuleKeys.Sales,
        ModuleKeys.ServiceOrder,
        ModuleKeys.Inventory,
        ModuleKeys.Financial,
        ModuleKeys.HR,
        ModuleKeys.Assets,
        ModuleKeys.Kanban,
        ModuleKeys.Reports,
        ModuleKeys.Admin
    };
    
    public async Task<IReadOnlyList<ModulePermission>> GetAllModulesAsync()
    {
        if (_cache.TryGetValue(AllModulesCacheKey, out IReadOnlyList<ModulePermission>? cached) && cached != null)
        {
            return cached;
        }
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        var modules = await context.ModulePermissions
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
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        var modules = await context.RoleModulePermissions
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
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var existingAssignments = await context.RoleModulePermissions
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
            
        await context.RoleModulePermissions.AddRangeAsync(newAssignments);
        await context.SaveChangesAsync();
        
        InvalidateRoleCache(roleId);
    }
    
    public async Task UpdateRoleModulesAsync(int roleId, IEnumerable<int> modulePermissionIds, int? grantedByUserId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Remove existing assignments
        var existingAssignments = await context.RoleModulePermissions
            .Where(rmp => rmp.RoleId == roleId)
            .ToListAsync();
            
        context.RoleModulePermissions.RemoveRange(existingAssignments);
        
        // Add new assignments
        var newAssignments = modulePermissionIds.Select(mpId => new RoleModulePermission
        {
            RoleId = roleId,
            ModulePermissionId = mpId,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        });
        
        await context.RoleModulePermissions.AddRangeAsync(newAssignments);
        await context.SaveChangesAsync();
        
        InvalidateRoleCache(roleId);
        InvalidateRoleActionCache(roleId);
        InvalidateAllUserCaches();
    }

    public async Task<bool> HasModuleActionAccessAsync(int userId, string moduleKey, string actionKey)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var roleNames = await _userManager.GetRolesAsync(user);
        if (roleNames.Contains("Administrador"))
            return true;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var actionExists = await context.ModuleActionPermissions
            .AnyAsync(a =>
                a.IsActive &&
                a.ActionKey == actionKey &&
                a.ModulePermission.ModuleKey == moduleKey);

        // Backward-compatible fallback for modules/actions not configured yet.
        if (!actionExists)
            return await HasModuleAccessAsync(userId, moduleKey);

        var roleIds = await context.Set<ApplicationRole>()
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        return await context.RoleModuleActionPermissions
            .AnyAsync(p =>
                roleIds.Contains(p.RoleId) &&
                p.ModuleActionPermission.IsActive &&
                p.ModuleActionPermission.ActionKey == actionKey &&
                p.ModuleActionPermission.ModulePermission.ModuleKey == moduleKey);
    }

    public async Task<bool> HasModuleActionAccessAsync(ClaimsPrincipal user, string moduleKey, string actionKey)
    {
        if (user.IsInRole("Administrador"))
            return true;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return false;

        return await HasModuleActionAccessAsync(userId, moduleKey, actionKey);
    }

    public async Task<IReadOnlyList<int>> GetRoleModuleActionIdsAsync(int roleId)
    {
        var cacheKey = $"{RoleModuleActionsCacheKeyPrefix}{roleId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<int>? cached) && cached != null)
            return cached;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var actionIds = await context.RoleModuleActionPermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.ModuleActionPermissionId)
            .ToListAsync();

        _cache.Set(cacheKey, (IReadOnlyList<int>)actionIds, CacheDuration);
        return actionIds;
    }

    public async Task UpdateRoleModuleActionsAsync(int roleId, IEnumerable<int> moduleActionPermissionIds, int? grantedByUserId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingAssignments = await context.RoleModuleActionPermissions
            .Where(x => x.RoleId == roleId)
            .ToListAsync();

        context.RoleModuleActionPermissions.RemoveRange(existingAssignments);

        var ids = moduleActionPermissionIds.Distinct().ToList();
        var newAssignments = ids.Select(actionId => new RoleModuleActionPermission
        {
            RoleId = roleId,
            ModuleActionPermissionId = actionId,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        });

        await context.RoleModuleActionPermissions.AddRangeAsync(newAssignments);
        await context.SaveChangesAsync();

        InvalidateRoleActionCache(roleId);
        InvalidateAllUserCaches();
    }
    
    private void InvalidateRoleCache(int roleId)
    {
        _cache.Remove($"{RoleModulesCacheKeyPrefix}{roleId}");
    }

    private void InvalidateRoleActionCache(int roleId)
    {
        _cache.Remove($"{RoleModuleActionsCacheKeyPrefix}{roleId}");
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
