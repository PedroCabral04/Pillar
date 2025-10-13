using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Inventory;

public class StockCountDto
{
    public int Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    
    public List<StockCountItemDto> Items { get; set; } = new();
    
    public string? Notes { get; set; }
    
    // Estatísticas calculadas
    public int TotalItems => Items.Count;
    public int ItemsWithDifference => Items.Count(i => i.Difference != 0);
    public decimal TotalDifferenceValue => Items.Sum(i => i.Difference * i.UnitCost);
}

public class StockCountItemDto
{
    public int Id { get; set; }
    public int StockCountId { get; set; }
    
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    
    public decimal SystemStock { get; set; }
    public decimal PhysicalStock { get; set; }
    public decimal Difference { get; set; }
    public decimal DifferencePercentage { get; set; }
    
    public decimal UnitCost { get; set; }
    public decimal DifferenceValue { get; set; }
    
    public string? Notes { get; set; }
}

public class CreateStockCountDto
{
    [StringLength(50, ErrorMessage = "Número da contagem deve ter no máximo 50 caracteres")]
    public string? CountNumber { get; set; }
    
    public int? WarehouseId { get; set; }
    
    [StringLength(1000, ErrorMessage = "Observações devem ter no máximo 1000 caracteres")]
    public string? Notes { get; set; }
    
    public DateTime CountDate { get; set; } = DateTime.UtcNow;
}

public class AddStockCountItemDto
{
    [Required(ErrorMessage = "ID da contagem é obrigatório")]
    public int StockCountId { get; set; }
    
    [Required(ErrorMessage = "Produto é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido")]
    public int ProductId { get; set; }
    
    [Required(ErrorMessage = "Estoque físico é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Estoque físico deve ser positivo")]
    public decimal PhysicalStock { get; set; }
    
    [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Notes { get; set; }
}

public class ApproveStockCountDto
{
    [Required(ErrorMessage = "ID da contagem é obrigatório")]
    public int StockCountId { get; set; }
    
    public bool ApplyAdjustments { get; set; } = true;
}
