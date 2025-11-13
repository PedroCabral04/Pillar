namespace erp.DTOs.Reports;

/// <summary>
/// DTO for inventory report filters
/// </summary>
public class InventoryReportFilterDto : ReportFilterDto
{
    public string ReportType { get; set; } = "stock-levels"; // stock-levels, movement-history, valuation, low-stock
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int? WarehouseId { get; set; }
    public string? MovementType { get; set; }
    public bool? OnlyLowStock { get; set; }
}

/// <summary>
/// DTO for stock levels report
/// </summary>
public class StockLevelsReportDto
{
    public List<StockLevelItemDto> Items { get; set; } = new();
    public StockLevelsSummaryDto Summary { get; set; } = new();
}

public class StockLevelItemDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TotalValue { get; set; }
    public string Status { get; set; } = string.Empty; // OK, Baixo, Crítico
}

public class StockLevelsSummaryDto
{
    public int TotalProducts { get; set; }
    public int ProductsInStock { get; set; }
    public int ProductsLowStock { get; set; }
    public int ProductsOutOfStock { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

/// <summary>
/// DTO for stock movement history report
/// </summary>
public class StockMovementReportDto
{
    public List<StockMovementItemDto> Items { get; set; } = new();
    public StockMovementSummaryDto Summary { get; set; } = new();
}

public class StockMovementItemDto
{
    public int MovementId { get; set; }
    public DateTime Date { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? ReferenceDocument { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class StockMovementSummaryDto
{
    public int TotalMovements { get; set; }
    public Dictionary<string, int> MovementsByType { get; set; } = new();
    public decimal TotalValueMovements { get; set; }
}

/// <summary>
/// DTO for inventory valuation report
/// </summary>
public class InventoryValuationReportDto
{
    public List<InventoryValuationItemDto> Items { get; set; } = new();
    public InventoryValuationSummaryDto Summary { get; set; } = new();
}

public class InventoryValuationItemDto
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal PotentialProfit { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class InventoryValuationSummaryDto
{
    public decimal TotalCostValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal TotalPotentialProfit { get; set; }
    public decimal AverageProfitMargin { get; set; }
}
