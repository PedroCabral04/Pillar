namespace erp.DTOs.Inventory;

public class ProductSearchDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? Status { get; set; }
    public bool? LowStock { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class StockAlertDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string AlertLevel { get; set; } = string.Empty;
    public DateTime LastMovementDate { get; set; }
}

public class BulkUpdatePriceDto
{
    public List<int> ProductIds { get; set; } = new();
    public decimal? CostPriceAdjustment { get; set; }
    public decimal? SalePriceAdjustment { get; set; }
    public bool AdjustmentIsPercentage { get; set; } = true;
}
