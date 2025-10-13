namespace erp.Models.Inventory;

public class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
