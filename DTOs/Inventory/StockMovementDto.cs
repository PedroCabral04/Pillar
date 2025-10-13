using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Inventory;

public class StockMovementDto
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    
    public int Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    
    public int Reason { get; set; }
    public string ReasonName { get; set; } = string.Empty;
    
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal CurrentStock { get; set; }
    
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    
    public string? DocumentNumber { get; set; }
    public int? SaleOrderId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? TransferId { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime MovementDate { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public class CreateStockMovementDto
{
    [Required(ErrorMessage = "Produto é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido")]
    public int ProductId { get; set; }
    
    public int? WarehouseId { get; set; }
    
    [Required(ErrorMessage = "Tipo de movimentação é obrigatório")]
    public int Type { get; set; }
    
    [Required(ErrorMessage = "Motivo é obrigatório")]
    public int Reason { get; set; }
    
    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public decimal Quantity { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Custo unitário deve ser positivo")]
    public decimal UnitCost { get; set; } = 0;
    
    [StringLength(100, ErrorMessage = "Número do documento deve ter no máximo 100 caracteres")]
    public string? DocumentNumber { get; set; }
    
    public int? SaleOrderId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? TransferId { get; set; }
    
    [StringLength(1000, ErrorMessage = "Observações devem ter no máximo 1000 caracteres")]
    public string? Notes { get; set; }
    
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
}

public class CreateStockAdjustmentDto
{
    [Required(ErrorMessage = "Produto é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido")]
    public int ProductId { get; set; }
    
    public int? WarehouseId { get; set; }
    
    [Required(ErrorMessage = "Novo estoque é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Novo estoque deve ser positivo ou zero")]
    public decimal NewStock { get; set; }
    
    [Required(ErrorMessage = "Motivo do ajuste é obrigatório")]
    [StringLength(500, ErrorMessage = "Motivo deve ter no máximo 500 caracteres")]
    public string Reason { get; set; } = string.Empty;
}
