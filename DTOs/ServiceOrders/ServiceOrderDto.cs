using erp.DTOs.Sales;

namespace erp.DTOs.ServiceOrders;

/// <summary>
/// DTO para representação completa de ordem de serviço
/// </summary>
public class ServiceOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public CustomerMiniDto? Customer { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;

    // ===== Informações do Aparelho =====

    public string? DeviceBrand { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceType { get; set; }
    public string? SerialNumber { get; set; }
    public string? Password { get; set; }
    public string? Accessories { get; set; }

    // ===== Descrições =====

    public string? ProblemDescription { get; set; }
    public string? TechnicalNotes { get; set; }
    public string? CustomerNotes { get; set; }

    // ===== Informações Financeiras =====

    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }

    // ===== Datas de Conclusão =====

    public DateTime? EstimatedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }

    // ===== Garantia =====

    public string? WarrantyType { get; set; }
    public DateTime? WarrantyExpiration { get; set; }

    // ===== Itens =====

    public List<ServiceOrderItemDto> Items { get; set; } = new();

    // ===== Auditoria =====

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO resumido de cliente para uso em ServiceOrder
/// </summary>
public class CustomerMiniDto
{
    public int Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
}

/// <summary>
/// DTO para resposta paginada de ordens de serviço
/// </summary>
public class PaginatedServiceOrdersResponse
{
    public List<ServiceOrderDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// DTO para atualização de status
/// </summary>
public class UpdateStatusDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Status é obrigatório")]
    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

/// <summary>
/// DTO para resumo de status
/// </summary>
public class ServiceOrderStatusSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}
