using System.ComponentModel.DataAnnotations;

namespace erp.Models.Payroll;

/// <summary>
/// Representa um item detalhado da folha (provento/desconto).
/// </summary>
public class PayrollComponent
{
    public int Id { get; set; }

    public int PayrollResultId { get; set; }
    public PayrollResult PayrollResult { get; set; } = null!;

    public PayrollComponentType Type { get; set; }

    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor monetário (positivo). Para descontos o Type controla o sinal.
    /// </summary>
    [Range(typeof(decimal), "-99999999", "99999999")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Base de cálculo utilizada (por exemplo salário base para INSS).
    /// </summary>
    public decimal? BaseAmount { get; set; }

    /// <summary>
    /// Quantidade/hora utilizada no cálculo (opcional).
    /// </summary>
    public decimal? ReferenceQuantity { get; set; }

    public bool ImpactsFgts { get; set; }
    public bool IsTaxable { get; set; } = true;

    /// <summary>
    /// Ordem para exibição.
    /// </summary>
    public int Sequence { get; set; }
}
