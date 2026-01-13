using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using erp.Models.Audit;

namespace erp.Models.ServiceOrders;

/// <summary>
/// Representa um item de serviço dentro de uma ordem de serviço
/// </summary>
public class ServiceOrderItem : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int ServiceOrderId { get; set; }

    /// <summary>
    /// Descrição do serviço realizado
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do serviço (Hardware, Software, Diagnóstico, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ServiceType { get; set; }

    /// <summary>
    /// Preço do serviço
    /// </summary>
    [Precision(18, 2)]
    public decimal Price { get; set; }

    /// <summary>
    /// Detalhes técnicos adicionais sobre o serviço
    /// </summary>
    [MaxLength(2000)]
    public string? TechnicalDetails { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ServiceOrder ServiceOrder { get; set; } = null!;
}
