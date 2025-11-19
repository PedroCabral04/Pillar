using erp.Models.Audit;
using System.ComponentModel.DataAnnotations;

namespace erp.Models.Tenancy;

public class TenantBranding : IAuditable
{
    public int Id { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? FaviconUrl { get; set; }

    [MaxLength(20)]
    public string? PrimaryColor { get; set; }

    [MaxLength(20)]
    public string? SecondaryColor { get; set; }

    [MaxLength(20)]
    public string? AccentColor { get; set; }

    [MaxLength(500)]
    public string? LoginBackgroundUrl { get; set; }

    [MaxLength(2000)]
    public string? EmailFooterHtml { get; set; }

    [MaxLength(2000)]
    public string? CustomCss { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
