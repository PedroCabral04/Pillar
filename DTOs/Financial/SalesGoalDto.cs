using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Financial;

/// <summary>
/// DTO para exibição de metas de vendas
/// </summary>
public class SalesGoalDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TargetSalesAmount { get; set; }
    public decimal TargetProfitAmount { get; set; }
    public int TargetSalesCount { get; set; }
    public decimal BonusCommissionPercent { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para criação de metas de vendas
/// </summary>
public class CreateSalesGoalDto
{
    [Required(ErrorMessage = "O TenantId é obrigatório")]
    public int TenantId { get; set; }

    [Required(ErrorMessage = "O usuário é obrigatório")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "O ano é obrigatório")]
    [Range(2020, 2100, ErrorMessage = "Ano deve estar entre 2020 e 2100")]
    public int Year { get; set; }

    [Required(ErrorMessage = "O mês é obrigatório")]
    [Range(1, 12, ErrorMessage = "Mês deve estar entre 1 e 12")]
    public int Month { get; set; }

    [Required(ErrorMessage = "A meta de vendas é obrigatória")]
    [Range(0.01, double.MaxValue, ErrorMessage = "A meta de vendas deve ser maior que zero")]
    public decimal TargetSalesAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "A meta de lucro não pode ser negativa")]
    public decimal TargetProfitAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A meta de quantidade deve ser maior que zero")]
    public int TargetSalesCount { get; set; }

    [Range(0, 100, ErrorMessage = "O bônus de comissão deve estar entre 0 e 100")]
    public decimal BonusCommissionPercent { get; set; }

    [MaxLength(500, ErrorMessage = "As observações não podem exceder 500 caracteres")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "O usuário criador é obrigatório")]
    public int CreatedByUserId { get; set; }
}

/// <summary>
/// DTO para atualização de metas de vendas
/// </summary>
public class UpdateSalesGoalDto
{
    [Required(ErrorMessage = "A meta de vendas é obrigatória")]
    [Range(0.01, double.MaxValue, ErrorMessage = "A meta de vendas deve ser maior que zero")]
    public decimal TargetSalesAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "A meta de lucro não pode ser negativa")]
    public decimal TargetProfitAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A meta de quantidade deve ser maior que zero")]
    public int TargetSalesCount { get; set; }

    [Range(0, 100, ErrorMessage = "O bônus de comissão deve estar entre 0 e 100")]
    public decimal BonusCommissionPercent { get; set; }

    [MaxLength(500, ErrorMessage = "As observações não podem exceder 500 caracteres")]
    public string? Notes { get; set; }
}
