using System.ComponentModel.DataAnnotations;
using erp.Models.Payroll;

namespace erp.DTOs.Payroll;

public class PayrollPeriodListDto
{
    public int Id { get; set; }
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalInssAmount { get; set; }
    public decimal TotalIrrfAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CalculationDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int TotalEmployees { get; set; }
}

public class PayrollPeriodDetailDto : PayrollPeriodListDto
{
    public string? Notes { get; set; }
    public List<PayrollResultDto> Results { get; set; } = new();
}

public class PayrollResultDto
{
    public int Id { get; set; }
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeCpf { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetAmount { get; set; }
    public decimal InssAmount { get; set; }
    public decimal IrrfAmount { get; set; }
    public decimal? AdditionalEmployerCost { get; set; }
    public DateTime? PaymentDate { get; set; }
    public List<PayrollComponentDto> Components { get; set; } = new();
    public PayrollSlipDto? Slip { get; set; }
}

public class PayrollComponentDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public decimal? BaseAmount { get; set; }
    public decimal? ReferenceQuantity { get; set; }
    public bool ImpactsFgts { get; set; }
    public bool IsTaxable { get; set; }
    public int Sequence { get; set; }
}

public class PayrollSlipDto
{
    public int Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
}

public class CreatePayrollPeriodRequest
{
    [Range(1, 12)]
    public int ReferenceMonth { get; set; }

    [Range(2000, 2100)]
    public int ReferenceYear { get; set; }
}

public class ApprovePayrollPeriodRequest
{
    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class PayPayrollPeriodRequest
{
    [Required]
    public DateTime PaymentDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class CalculatePayrollPeriodRequest
{
    /// <summary>
    /// Modo de cálculo da folha. Simplified não calcula INSS/IRRF. Full calcula todos os impostos.
    /// </summary>
    public PayrollCalculationMode Mode { get; set; } = PayrollCalculationMode.Full;
}
