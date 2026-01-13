using erp.Models.Identity;
using erp.Models;

namespace erp.Models.Inventory;

public class StockCount : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedDate { get; set; }
    public StockCountStatus Status { get; set; } = StockCountStatus.InProgress;
    
    public int? WarehouseId { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
    
    // Responsáveis
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
    public int? ApprovedByUserId { get; set; }
    public virtual ApplicationUser? ApprovedByUser { get; set; }
    
    // Itens contados
    public virtual ICollection<StockCountItem> Items { get; set; } = new List<StockCountItem>();
    
    public string? Notes { get; set; }
}
