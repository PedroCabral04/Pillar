using Microsoft.AspNetCore.Identity;

namespace erp.Models.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public bool IsActive { get; set; } = true;
    public string? PreferencesJson { get; set; }
}
