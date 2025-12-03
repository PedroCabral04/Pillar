using Microsoft.AspNetCore.Authorization;

namespace erp.Services.Authorization;

/// <summary>
/// Requirement for accessing a specific module
/// </summary>
public class ModuleAccessRequirement : IAuthorizationRequirement
{
    public string ModuleKey { get; }
    
    public ModuleAccessRequirement(string moduleKey)
    {
        ModuleKey = moduleKey;
    }
}

/// <summary>
/// Handler for module access authorization
/// </summary>
public class ModuleAccessHandler : AuthorizationHandler<ModuleAccessRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<ModuleAccessHandler> _logger;
    
    public ModuleAccessHandler(
        IPermissionService permissionService,
        ILogger<ModuleAccessHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleAccessRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogDebug("User not authenticated, denying access to module {Module}", requirement.ModuleKey);
            return;
        }
        
        // Administrators always have access
        if (context.User.IsInRole("Administrador"))
        {
            _logger.LogDebug("User is Administrator, granting access to module {Module}", requirement.ModuleKey);
            context.Succeed(requirement);
            return;
        }
        
        var hasAccess = await _permissionService.HasModuleAccessAsync(context.User, requirement.ModuleKey);
        
        if (hasAccess)
        {
            _logger.LogDebug("User has access to module {Module}", requirement.ModuleKey);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogDebug("User denied access to module {Module}", requirement.ModuleKey);
        }
    }
}

/// <summary>
/// Policy names for module access
/// </summary>
public static class ModulePolicies
{
    public const string Dashboard = "Module.Dashboard";
    public const string Sales = "Module.Sales";
    public const string Inventory = "Module.Inventory";
    public const string Financial = "Module.Financial";
    public const string HR = "Module.HR";
    public const string Assets = "Module.Assets";
    public const string Kanban = "Module.Kanban";
    public const string Reports = "Module.Reports";
    public const string Admin = "Module.Admin";
    
    /// <summary>
    /// Get policy name for a module key
    /// </summary>
    public static string GetPolicyName(string moduleKey)
    {
        return $"Module.{char.ToUpper(moduleKey[0])}{moduleKey[1..]}";
    }
}
