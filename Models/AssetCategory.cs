namespace erp.Models;

/// <summary>
/// Categoria de ativos (Computadores, Móveis, Veículos, etc.)
/// </summary>
public class AssetCategory : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? Icon { get; set; } // MudBlazor icon name
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
