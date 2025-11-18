using Microsoft.AspNetCore.Identity;

namespace erp.Models.Identity;

public class ApplicationRole : IdentityRole<int>
{
	public string? Abbreviation { get; set; }
    public int? TenantId { get; set; }
}
