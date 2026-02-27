using erp.Models.Audit;

namespace erp.Models.Inventory;

/// <summary>
/// Combinação concreta de variação de um produto com SKU, preço e estoque próprios.
/// Ex: "Camiseta Básica - Vermelho / G"
/// </summary>
public class ProductVariant : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    
    // Identificação
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    
    /// <summary>
    /// Nome gerado automaticamente a partir das opções selecionadas (ex: "Vermelho / G")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    // Precificação (override do produto pai)
    public decimal CostPrice { get; set; } = 0;
    public decimal SalePrice { get; set; } = 0;
    public decimal? WholesalePrice { get; set; }
    
    // Estoque individual
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinimumStock { get; set; } = 0;
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Imagem específica da variação
    public string? ImageUrl { get; set; }
    
    // Combinação de opções que definem esta variação
    public virtual ICollection<ProductVariantCombination> Combinations { get; set; } = new List<ProductVariantCombination>();
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
