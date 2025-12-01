using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Inventory;

public class CreateProductDto
{
    [Required(ErrorMessage = "SKU é obrigatório")]
    [StringLength(50, ErrorMessage = "SKU deve ter no máximo 50 caracteres")]
    public string Sku { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "Código de barras deve ter no máximo 50 caracteres")]
    public string? Barcode { get; set; }
    
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(2000, ErrorMessage = "Descrição deve ter no máximo 2000 caracteres")]
    public string? Description { get; set; }
    
    public string? TechnicalSpecifications { get; set; }
    
    [Required(ErrorMessage = "Categoria é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Categoria inválida")]
    public int CategoryId { get; set; }
    
    public int? BrandId { get; set; }
    public string? Tags { get; set; }
    
    [Required(ErrorMessage = "Unidade é obrigatória")]
    [StringLength(10, ErrorMessage = "Unidade deve ter no máximo 10 caracteres")]
    public string Unit { get; set; } = "UN";
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Unidades por caixa deve ser maior que zero")]
    public decimal UnitsPerBox { get; set; } = 1;
    
    [Range(0, double.MaxValue, ErrorMessage = "Peso deve ser positivo")]
    public decimal? Weight { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Comprimento deve ser positivo")]
    public decimal? Length { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Largura deve ser positiva")]
    public decimal? Width { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Altura deve ser positiva")]
    public decimal? Height { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque mínimo deve ser positivo")]
    public decimal MinimumStock { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque máximo deve ser positivo")]
    public decimal MaximumStock { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque inicial deve ser positivo")]
    public decimal CurrentStock { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Ponto de reposição deve ser positivo")]
    public decimal ReorderPoint { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque de segurança deve ser positivo")]
    public decimal SafetyStock { get; set; } = 0;
    
    [StringLength(100, ErrorMessage = "Localização deve ter no máximo 100 caracteres")]
    public string? WarehouseLocation { get; set; }
    
    [Required(ErrorMessage = "Preço de custo é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Preço de custo deve ser positivo")]
    public decimal CostPrice { get; set; } = 0;
    
    [Required(ErrorMessage = "Preço de venda é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Preço de venda deve ser positivo")]
    public decimal SalePrice { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço atacado deve ser positivo")]
    public decimal? WholesalePrice { get; set; }
    
    [StringLength(10, ErrorMessage = "Código NCM deve ter no máximo 10 caracteres")]
    public string? NcmCode { get; set; }
    
    [StringLength(10, ErrorMessage = "Código CEST deve ter no máximo 10 caracteres")]
    public string? CestCode { get; set; }
    
    [Range(0, 100, ErrorMessage = "Taxa de ICMS deve estar entre 0 e 100")]
    public decimal IcmsRate { get; set; } = 0;
    
    [Range(0, 100, ErrorMessage = "Taxa de IPI deve estar entre 0 e 100")]
    public decimal IpiRate { get; set; } = 0;
    
    [Range(0, 100, ErrorMessage = "Taxa de PIS deve estar entre 0 e 100")]
    public decimal PisRate { get; set; } = 0;
    
    [Range(0, 100, ErrorMessage = "Taxa de COFINS deve estar entre 0 e 100")]
    public decimal CofinsRate { get; set; } = 0;
    
    public int Status { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = false;
    public bool IsKit { get; set; } = false;
    
    public string? MainImageUrl { get; set; }
}
