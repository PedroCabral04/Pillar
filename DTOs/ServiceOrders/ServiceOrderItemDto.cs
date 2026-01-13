namespace erp.DTOs.ServiceOrders;

/// <summary>
/// DTO para representação de um item de ordem de serviço
/// </summary>
public class ServiceOrderItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ServiceType { get; set; }
    public decimal Price { get; set; }
    public string? TechnicalDetails { get; set; }
}

/// <summary>
/// DTO para criação de item de ordem de serviço
/// </summary>
public class CreateServiceOrderItemDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Descrição do serviço é obrigatória")]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? ServiceType { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Preço do serviço é obrigatório")]
    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue, ErrorMessage = "Preço deve ser positivo")]
    public decimal Price { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? TechnicalDetails { get; set; }
}

/// <summary>
/// DTO para atualização de item de ordem de serviço
/// </summary>
public class UpdateServiceOrderItemDto
{
    /// <summary>
    /// Id do item existente. Se null ou 0, será criado um novo item.
    /// </summary>
    public int? Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Descrição do serviço é obrigatória")]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? ServiceType { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Preço do serviço é obrigatório")]
    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue, ErrorMessage = "Preço deve ser positivo")]
    public decimal Price { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? TechnicalDetails { get; set; }
}
