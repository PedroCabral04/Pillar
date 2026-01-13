namespace erp.DTOs.ServiceOrders;

/// <summary>
/// DTO para criação de ordem de serviço
/// </summary>
public class CreateServiceOrderDto
{
    public int? CustomerId { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Data de entrada é obrigatória")]
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Status é obrigatório")]
    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string Status { get; set; } = "Open";

    // ===== Informações do Aparelho =====

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? DeviceBrand { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? DeviceModel { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? DeviceType { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? SerialNumber { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(20)]
    public string? Password { get; set; }

    public string? Accessories { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? ProblemDescription { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? TechnicalNotes { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? CustomerNotes { get; set; }

    // ===== Informações Financeiras =====

    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue, ErrorMessage = "Desconto deve ser positivo")]
    public decimal DiscountAmount { get; set; }

    // ===== Datas de Conclusão =====

    public DateTime? EstimatedCompletionDate { get; set; }

    // ===== Garantia =====

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? WarrantyType { get; set; }

    public DateTime? WarrantyExpiration { get; set; }

    // ===== Itens =====

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A ordem de serviço deve conter pelo menos um item")]
    [System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "A ordem de serviço deve conter pelo menos um item")]
    public List<CreateServiceOrderItemDto> Items { get; set; } = new();
}
