using erp.Models.Audit;

namespace erp.Models.Payroll;

/// <summary>
/// Faixa progressiva para INSS/IRRF.
/// </summary>
public class PayrollTaxBracket : IAuditable
{
    public int Id { get; set; }
    public PayrollTaxType TaxType { get; set; }
    public decimal RangeStart { get; set; }
    public decimal? RangeEnd { get; set; }
    public decimal Rate { get; set; }
    public decimal Deduction { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveTo { get; set; }
    public int SortOrder { get; set; }
}
