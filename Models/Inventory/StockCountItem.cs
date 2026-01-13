namespace erp.Models.Inventory;

public class StockCountItem : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public int StockCountId { get; set; }
    public virtual StockCount StockCount { get; set; } = null!;
    
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    
    public decimal SystemStock { get; set; } = 0;
    public decimal PhysicalStock { get; set; } = 0;
    
    public string? Notes { get; set; }
}
