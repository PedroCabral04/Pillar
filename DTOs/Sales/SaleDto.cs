using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Sales;

public class SaleDto
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SaleItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
}

public class CreateSaleDto
{
    public int? CustomerId { get; set; }

    /// <summary>
    /// Vendedor responsável pela venda (opcional - se não informado, usa o usuário logado)
    /// </summary>
    public int? UserId { get; set; }

    [Required(ErrorMessage = "Data da venda é obrigatória")]
    public DateTime SaleDate { get; set; } = DateTime.Now;
    
    [Range(0, double.MaxValue, ErrorMessage = "Desconto deve ser positivo")]
    public decimal DiscountAmount { get; set; }
    
    [Required(ErrorMessage = "Status é obrigatório")]
    [StringLength(20)]
    public string Status { get; set; } = "Pendente";
    
    [StringLength(50)]
    public string? PaymentMethod { get; set; }
    
    public string? Notes { get; set; }
    
    [Required(ErrorMessage = "A venda deve conter pelo menos um item")]
    [MinLength(1, ErrorMessage = "A venda deve conter pelo menos um item")]
    public List<CreateSaleItemDto> Items { get; set; } = new();
}

public class CreateSaleItemDto
{
    [Required(ErrorMessage = "Produto é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido")]
    public int ProductId { get; set; }
    
    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Preço unitário é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "Preço deve ser positivo")]
    public decimal UnitPrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Desconto deve ser positivo")]
    public decimal Discount { get; set; }
}

public class UpdateSaleDto
{
    public int? CustomerId { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Desconto deve ser positivo")]
    public decimal DiscountAmount { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pendente";
    
    [StringLength(50)]
    public string? PaymentMethod { get; set; }
    
    public string? Notes { get; set; }
}
