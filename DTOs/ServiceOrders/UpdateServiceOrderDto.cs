namespace erp.DTOs.ServiceOrders;

/// <summary>
/// DTO para atualização de ordem de serviço
/// </summary>
public class UpdateServiceOrderDto
{
    public int? CustomerId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string Status { get; set; } = "Open";

    // ===== Informações do Aparelho (não editáveis após criação) =====
    // DeviceBrand, DeviceModel, DeviceType, SerialNumber, Password, Accessories não são editáveis

    // ===== Notas =====

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? ProblemDescription { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? TechnicalNotes { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? CustomerNotes { get; set; }

    // ===== Informações Financeiras =====

    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? PaymentMethod { get; set; }

    // ===== Datas de Conclusão =====

    public DateTime? EstimatedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }

    // ===== Garantia =====

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? WarrantyType { get; set; }

    public DateTime? WarrantyExpiration { get; set; }

    // ===== Itens =====

    public List<UpdateServiceOrderItemDto>? Items { get; set; }
}
