using erp.Models.Identity;

namespace erp.Models.Inventory;

public class StockMovement
{
    public int Id { get; set; }
    
    // Relacionamentos
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public int? WarehouseId { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
    
    // Tipo de Movimentação
    public MovementType Type { get; set; }
    public MovementReason Reason { get; set; }
    
    // Quantidades
    public decimal Quantity { get; set; } = 0;
    public decimal PreviousStock { get; set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    
    // Valores
    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;
    
    // Referências
    public string? DocumentNumber { get; set; }
    public int? SaleOrderId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? TransferId { get; set; }
    
    // Observações
    public string? Notes { get; set; }
    
    // Metadata
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
}
