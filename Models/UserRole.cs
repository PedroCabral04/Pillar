namespace erp.Models;

/// <summary>
/// Join table for User-Role many-to-many relationship.
/// </summary>
/// <remarks>
/// OBSOLETE: Use <see cref="Microsoft.AspNetCore.Identity.IdentityUserRole{TKey}"/> instead.
/// This legacy UserRole model is kept for backwards compatibility only.
/// New code should use the IdentityUserRole from ASP.NET Core Identity.
/// </remarks>
[Obsolete("Use IdentityUserRole<int> from ASP.NET Core Identity instead. This legacy model will be removed in future versions.")]
public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
} 