namespace erp.Models.Inventory;

public class ProductSupplier
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    
    // Por enquanto usamos SupplierId como int, depois pode ser FK para entidade Supplier
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    
    public string? SupplierProductCode { get; set; }
    public decimal SupplierCostPrice { get; set; } = 0;
    public int LeadTimeDays { get; set; } = 0;
    public decimal MinimumOrderQuantity { get; set; } = 1;
    public bool IsPreferred { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
