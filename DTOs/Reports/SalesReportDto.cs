namespace erp.DTOs.Reports;

/// <summary>
/// DTO for sales report filters
/// </summary>
public class SalesReportFilterDto : ReportFilterDto
{
    public int? CustomerId { get; set; }
    public int? ProductId { get; set; }
    public int? SalespersonId { get; set; }
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// DTO for sales report results
/// </summary>
public class SalesReportResultDto
{
    // New structure
    public List<SalesReportItemDto> Items { get; set; } = new();
    public SalesReportSummaryDto Summary { get; set; } = new();

    // Backwards-compatible convenience properties (legacy tests expect these)
    public int TotalSales => Summary.TotalSales;
    public decimal TotalAmount => Summary.TotalRevenue;
    public decimal TotalDiscount => Summary.TotalDiscounts;
    public decimal AverageTicket => Summary.AverageTicket;
    public List<erp.Models.Sales.Sale> Sales { get; set; } = new();
}

/// <summary>
/// Individual sale item in the report
/// </summary>
public class SalesReportItemDto
{
    public int SaleId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string SalespersonName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Summary statistics for sales report
/// </summary>
public class SalesReportSummaryDto
{
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal AverageTicket { get; set; }
    public int TotalItemsSold { get; set; }
    public Dictionary<string, int> SalesByStatus { get; set; } = new();
    public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
}

// Grouped by Customer
public class CustomerSalesReportResultDto
{
    public int TotalCustomers { get; set; }
    public List<CustomerSalesReportItemDto> Customers { get; set; } = new();
}

public class CustomerSalesReportItemDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
}

// Grouped by Product
public class ProductSalesReportResultDto
{
    public int TotalProducts { get; set; }
    public List<ProductSalesReportItemDto> Products { get; set; } = new();
}

public class ProductSalesReportItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

// Grouped by Payment Method
public class PaymentMethodSalesReportResultDto
{
    public int TotalMethods { get; set; }
    public List<PaymentMethodSalesReportItemDto> PaymentMethods { get; set; } = new();
}

public class PaymentMethodSalesReportItemDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
}
