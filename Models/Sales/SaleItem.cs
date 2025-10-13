using Microsoft.EntityFrameworkCore;
using erp.Models.Inventory;

namespace erp.Models.Sales;

/// <summary>
/// Represents an item within a sale
/// </summary>
public class SaleItem
{
    public int Id { get; set; }
    
    public int SaleId { get; set; }
    
    public int ProductId { get; set; }
    
    [Precision(18, 3)]
    public decimal Quantity { get; set; }
    
    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }
    
    [Precision(18, 2)]
    public decimal Discount { get; set; }
    
    [Precision(18, 2)]
    public decimal Total { get; set; }
    
    // Navigation properties
    public Sale Sale { get; set; } = null!;
    
    public Product Product { get; set; } = null!;
}
