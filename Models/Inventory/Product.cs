using erp.Models.Identity;
using erp.Models.Audit;

namespace erp.Models.Inventory;

public class Product : IAuditable
{
    public int Id { get; set; }
    
    // Identificação
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TechnicalSpecifications { get; set; }
    
    // Categorização
    public int CategoryId { get; set; }
    public virtual ProductCategory Category { get; set; } = null!;
    public int? BrandId { get; set; }
    public virtual Brand? Brand { get; set; }
    public string? Tags { get; set; }
    
    // Unidades
    public string Unit { get; set; } = "UN";
    public decimal UnitsPerBox { get; set; } = 1;
    
    // Dimensões (para cálculo de frete)
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    
    // Estoque
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinimumStock { get; set; } = 0;
    public decimal MaximumStock { get; set; } = 0;
    public decimal ReorderPoint { get; set; } = 0;
    public decimal SafetyStock { get; set; } = 0;
    
    // Localização
    public string? WarehouseLocation { get; set; }
    
    // Precificação
    public decimal CostPrice { get; set; } = 0;
    public decimal SalePrice { get; set; } = 0;
    public decimal? WholesalePrice { get; set; }
    
    // Fiscais
    public string? NcmCode { get; set; }
    public string? CestCode { get; set; }
    public decimal IcmsRate { get; set; } = 0;
    public decimal IpiRate { get; set; } = 0;
    public decimal PisRate { get; set; } = 0;
    public decimal CofinsRate { get; set; } = 0;
    
    // Status
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public bool IsActive { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = false;
    public bool IsKit { get; set; } = false;
    
    // Fornecedores
    public virtual ICollection<ProductSupplier> Suppliers { get; set; } = new List<ProductSupplier>();
    
    // Imagens
    public string? MainImageUrl { get; set; }
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    
    // Movimentações
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
}
