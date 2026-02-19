using System.Security.Claims;

namespace erp.Security;

public static class RoleNames
{
    public const string AdminTenant = "AdminTenant";
    public const string SuperAdmin = "SuperAdmin";

    // Backward compatibility for existing databases/users.
    public const string LegacyAdministrator = "Administrador";

    public const string AdminTenantOrSuperAdmin = AdminTenant + "," + SuperAdmin;
    public const string AdminTenantSuperAdminOrManager = AdminTenant + "," + SuperAdmin + ",Gerente";
    public const string AdminTenantSuperAdminManagerOrHR = AdminTenant + "," + SuperAdmin + ",Gerente,RH";

    public static bool IsAdminRoleName(string? roleName)
    {
        return string.Equals(roleName, AdminTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, LegacyAdministrator, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSystemAdministratorRole(string? roleName)
    {
        return string.Equals(roleName, AdminTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAdminPrincipal(ClaimsPrincipal principal)
    {
        return principal.IsInRole(AdminTenant)
            || principal.IsInRole(SuperAdmin)
            || principal.IsInRole(LegacyAdministrator);
    }

    public static bool IsAdminRoleCollection(IEnumerable<string> roleNames)
    {
        return roleNames.Any(IsAdminRoleName);
    }
}
