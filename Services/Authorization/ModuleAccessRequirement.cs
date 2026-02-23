using Microsoft.AspNetCore.Authorization;
using erp.Security;

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
        if (RoleNames.IsAdminPrincipal(context.User))
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
/// Authorization policy names for module-based access control.
/// Policy names follow the format "Module.{ModuleName}" where ModuleName
/// is the PascalCase version of the module key. Use GetPolicyName() to
/// generate policy names dynamically from module keys.
/// </summary>
public static class ModulePolicies
{
    public const string Dashboard = "Module.Dashboard";
    public const string Sales = "Module.Sales";
    public const string ServiceOrder = "Module.ServiceOrder";
    public const string Inventory = "Module.Inventory";
    public const string Financial = "Module.Financial";
    public const string HR = "Module.HR";
    public const string Assets = "Module.Assets";
    public const string Kanban = "Module.Kanban";
    public const string Reports = "Module.Reports";
    public const string Admin = "Module.Admin";

    /// <summary>
    /// Get policy name for a module key. Handles kebab-case keys (e.g., "service-orders" -> "Module.ServiceOrder").
    /// </summary>
    public static string GetPolicyName(string moduleKey)
    {
        // Handle kebab-case: convert "service-orders" to "ServiceOrder"
        if (moduleKey.Contains('-'))
        {
            var parts = moduleKey.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var pascalCase = string.Concat(parts.Select(p => char.ToUpper(p[0]) + p[1..]));
            return $"Module.{pascalCase}";
        }
        // Handle simple case: "sales" -> "Sales"
        return $"Module.{char.ToUpper(moduleKey[0])}{moduleKey[1..]}";
    }
}
