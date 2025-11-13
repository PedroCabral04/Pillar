using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.TimeTracking;

public class PayrollPeriodSummaryDto
{
    public int Id { get; set; }
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PayrollPeriodDetailDto : PayrollPeriodSummaryDto
{
    public List<PayrollEntryDto> Entries { get; set; } = new();
}

public class PayrollEntryDto
{
    public int Id { get; set; }
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public decimal? Faltas { get; set; }
    public decimal? Abonos { get; set; }
    public decimal? HorasExtras { get; set; }
    public decimal? Atrasos { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedById { get; set; }
}

public class CreatePayrollPeriodDto
{
    [Range(1, 12, ErrorMessage = "Mês de referência inválido.")]
    public int ReferenceMonth { get; set; }

    [Range(2000, 2100, ErrorMessage = "Ano de referência inválido.")]
    public int ReferenceYear { get; set; }
}

public class UpdatePayrollEntryDto
{
    public decimal? Faltas { get; set; }
    public decimal? Abonos { get; set; }
    public decimal? HorasExtras { get; set; }
    public decimal? Atrasos { get; set; }

    [StringLength(1000, ErrorMessage = "Observações devem ter no máximo 1000 caracteres.")]
    public string? Observacoes { get; set; }
}
