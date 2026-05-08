using System.ComponentModel.DataAnnotations;
using erp.Models.Financial;

namespace erp.DTOs.Financial;

public class CreateEmployeePaymentDto
{
    [Required]
    public int? EmployeeId { get; set; }

    [Required]
    public EmployeePaymentType Type { get; set; }

    [Required]
    [Range(0.01, 99999999)]
    public decimal? Amount { get; set; }

    public decimal? DiscountAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, 12)]
    public int? ReferenceMonth { get; set; }

    [Required]
    [Range(2000, 2100)]
    public int? ReferenceYear { get; set; }

    public DateTime DueDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;

    public int? CategoryId { get; set; }
    public int? CostCenterId { get; set; }

    public int? ServiceOrderId { get; set; }
    public int? ServiceCount { get; set; }
    public decimal? ServiceUnitValue { get; set; }
    public decimal? ServicePercent { get; set; }
}

public class UpdateEmployeePaymentDto
{
    [Required]
    [Range(0.01, 99999999)]
    public decimal? Amount { get; set; }

    public decimal? DiscountAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime DueDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;

    public int? CategoryId { get; set; }
    public int? CostCenterId { get; set; }
}

public class PayEmployeePaymentDto
{
    [Required]
    public DateTime PaymentDate { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class GenerateServicePaymentsDto
{
    [Required]
    [Range(1, 12)]
    public int? ReferenceMonth { get; set; }

    [Required]
    [Range(2000, 2100)]
    public int? ReferenceYear { get; set; }

    public DateTime DueDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
}

public class EmployeePaymentDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public EmployeePaymentType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public AccountStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Description { get; set; }
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodDescription { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public int? ServiceOrderId { get; set; }
    public string? ServiceOrderNumber { get; set; }
    public int? ServiceCount { get; set; }
    public decimal? ServiceUnitValue { get; set; }
    public decimal? ServicePercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public int? PaidByUserId { get; set; }
    public string? PaidByUserName { get; set; }
}

public class EmployeePaymentSummaryDto
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public EmployeePaymentType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public AccountStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
}
