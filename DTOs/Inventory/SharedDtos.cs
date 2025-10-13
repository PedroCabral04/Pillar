using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Inventory;

/// <summary>
/// Resultado paginado genérico
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// DTO para atualização em massa de preços
/// </summary>
public class BulkUpdatePricesDto
{
    [Required(ErrorMessage = "Lista de produtos é obrigatória")]
    public List<int> ProductIds { get; set; } = new();

    [Required(ErrorMessage = "Tipo de preço é obrigatório")]
    public string PriceType { get; set; } = "Sale"; // Sale, Cost, Wholesale

    [Required(ErrorMessage = "Tipo de ajuste é obrigatório")]
    public string AdjustmentType { get; set; } = "Percentage"; // Percentage, Fixed

    [Required(ErrorMessage = "Valor do ajuste é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal AdjustmentValue { get; set; }
}

/// <summary>
/// Resultado de atualização em massa
/// </summary>
public class BulkUpdateResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => FailureCount == 0;
}

/// <summary>
/// DTO consolidado de alertas de estoque
/// </summary>
public class StockAlertsDto
{
    public int LowStockCount { get; set; }
    public int OverstockCount { get; set; }
    public int InactiveProductsCount { get; set; }
    
    public List<ProductDto> LowStockProducts { get; set; } = new();
    public List<ProductDto> OverstockProducts { get; set; } = new();
    public List<ProductDto> InactiveProducts { get; set; } = new();
    
    public decimal TotalStockValue { get; set; }
    public int TotalActiveProducts { get; set; }
}
