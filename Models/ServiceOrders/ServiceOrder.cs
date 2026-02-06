using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using erp.Models.Audit;
using erp.Models.Identity;

namespace erp.Models.ServiceOrders;

/// <summary>
/// Representa uma ordem de serviço para assistência técnica
/// </summary>
public class ServiceOrder : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    /// <summary>
    /// Número único da ordem de serviço (formato: OSyyyyMMdd-nnn)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Cliente que solicitou o serviço (opcional)
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Usuário que criou a ordem
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Data de entrada do aparelho
    /// </summary>
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Status atual da ordem de serviço
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = ServiceOrderStatus.Open.ToString();

    // ===== Informações do Aparelho =====

    /// <summary>
    /// Marca do aparelho
    /// </summary>
    [MaxLength(100)]
    public string? DeviceBrand { get; set; }

    /// <summary>
    /// Modelo do aparelho
    /// </summary>
    [MaxLength(100)]
    public string? DeviceModel { get; set; }

    /// <summary>
    /// Tipo do aparelho (Smartphone, Tablet, Notebook, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Número de série do aparelho
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Senha do aparelho (para desbloqueio durante teste)
    /// </summary>
    [MaxLength(20)]
    public string? Password { get; set; }

    /// <summary>
    /// Acessórios entregues junto (cabo, capa, caixa, etc.)
    /// Armazenado como JSON: {"charger": true, "case": false, "box": true}
    /// </summary>
    public string? Accessories { get; set; }

    /// <summary>
    /// Descrição do problema relatado pelo cliente
    /// </summary>
    [MaxLength(2000)]
    public string? ProblemDescription { get; set; }

    /// <summary>
    /// Notas técnicas internas (não visíveis ao cliente)
    /// </summary>
    [MaxLength(2000)]
    public string? TechnicalNotes { get; set; }

    /// <summary>
    /// Notas para o cliente (visíveis na ordem impressa)
    /// </summary>
    [MaxLength(2000)]
    public string? CustomerNotes { get; set; }

    // ===== Informações Financeiras =====

    /// <summary>
    /// Valor total dos serviços
    /// </summary>
    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Valor do desconto aplicado
    /// </summary>
    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Valor líquido a pagar
    /// </summary>
    [Precision(18, 2)]
    public decimal NetAmount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    // ===== Datas de Conclusão =====

    /// <summary>
    /// Data prevista para conclusão do serviço
    /// </summary>
    public DateTime? EstimatedCompletionDate { get; set; }

    /// <summary>
    /// Data real de conclusão do serviço
    /// </summary>
    public DateTime? ActualCompletionDate { get; set; }

    // ===== Garantia =====

    /// <summary>
    /// Tipo de garantia oferecida
    /// </summary>
    [MaxLength(50)]
    public string? WarrantyType { get; set; }

    /// <summary>
    /// Data de expiração da garantia
    /// </summary>
    public DateTime? WarrantyExpiration { get; set; }

    // ===== Auditoria =====

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Models.Sales.Customer? Customer { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();
}
