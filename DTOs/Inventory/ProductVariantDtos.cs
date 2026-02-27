using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Inventory;

// ===== Response DTOs =====

/// <summary>
/// DTO de resposta para uma opção de variação
/// </summary>
public class ProductVariantOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<ProductVariantOptionValueDto> Values { get; set; } = new();
}

/// <summary>
/// DTO de resposta para um valor de opção
/// </summary>
public class ProductVariantOptionValueDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Position { get; set; }
}

/// <summary>
/// DTO de resposta para uma variação concreta do produto
/// </summary>
public class ProductVariantDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public List<ProductVariantOptionValueDto> OptionValues { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// ===== Create DTOs =====

/// <summary>
/// DTO para criação de uma opção de variação com seus valores
/// </summary>
public class CreateProductVariantOptionDto
{
    [Required(ErrorMessage = "Nome da opção é obrigatório")]
    [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Name { get; set; } = string.Empty;
    
    public int Position { get; set; } = 0;
    
    [Required(ErrorMessage = "Pelo menos um valor é obrigatório")]
    [MinLength(1, ErrorMessage = "Pelo menos um valor é obrigatório")]
    public List<CreateProductVariantOptionValueDto> Values { get; set; } = new();
}

/// <summary>
/// DTO para criação de um valor de opção
/// </summary>
public class CreateProductVariantOptionValueDto
{
    [Required(ErrorMessage = "Valor é obrigatório")]
    [StringLength(100, ErrorMessage = "Valor deve ter no máximo 100 caracteres")]
    public string Value { get; set; } = string.Empty;
    
    public int Position { get; set; } = 0;
}

/// <summary>
/// DTO para criação de uma variação concreta
/// </summary>
public class CreateProductVariantDto
{
    [StringLength(50, ErrorMessage = "SKU deve ter no máximo 50 caracteres")]
    public string? Sku { get; set; }
    
    [StringLength(50, ErrorMessage = "Código de barras deve ter no máximo 50 caracteres")]
    public string? Barcode { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço de custo deve ser positivo")]
    public decimal CostPrice { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço de venda deve ser positivo")]
    public decimal SalePrice { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço atacado deve ser positivo")]
    public decimal? WholesalePrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque deve ser positivo")]
    public decimal CurrentStock { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque mínimo deve ser positivo")]
    public decimal MinimumStock { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// IDs dos valores de opção que compõem esta variação
    /// </summary>
    [Required(ErrorMessage = "Valores de opção são obrigatórios")]
    [MinLength(1, ErrorMessage = "Pelo menos um valor de opção é necessário")]
    public List<int> OptionValueIds { get; set; } = new();
}

// ===== Update DTOs =====

/// <summary>
/// DTO para atualização de uma variação existente
/// </summary>
public class UpdateProductVariantDto
{
    [Required(ErrorMessage = "ID é obrigatório")]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "SKU é obrigatório")]
    [StringLength(50, ErrorMessage = "SKU deve ter no máximo 50 caracteres")]
    public string Sku { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "Código de barras deve ter no máximo 50 caracteres")]
    public string? Barcode { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço de custo deve ser positivo")]
    public decimal CostPrice { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço de venda deve ser positivo")]
    public decimal SalePrice { get; set; } = 0;
    
    [Range(0, double.MaxValue, ErrorMessage = "Preço atacado deve ser positivo")]
    public decimal? WholesalePrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Estoque mínimo deve ser positivo")]
    public decimal MinimumStock { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
}
