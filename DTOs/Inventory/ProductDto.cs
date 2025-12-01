using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Inventory;

public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TechnicalSpecifications { get; set; }
    
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? Tags { get; set; }
    
    public string Unit { get; set; } = "UN";
    public decimal UnitsPerBox { get; set; }
    
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    
    public string? WarehouseLocation { get; set; }
    
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    public decimal ProfitMargin { get; set; }
    
    public string? NcmCode { get; set; }
    public string? CestCode { get; set; }
    public decimal IcmsRate { get; set; }
    public decimal IpiRate { get; set; }
    public decimal PisRate { get; set; }
    public decimal CofinsRate { get; set; }
    
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool IsKit { get; set; }
    
    public string? MainImageUrl { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductSupplierDto> Suppliers { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Position { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductSupplierDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierProductCode { get; set; }
    public decimal SupplierCostPrice { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool IsPreferred { get; set; }
}

/// <summary>
/// Estatísticas gerais de produtos para exibição em dashboard/KPIs
/// </summary>
public class ProductStatisticsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int OverstockProducts { get; set; }
    public decimal TotalStockValue { get; set; }
    public int TotalCategories { get; set; }
}
