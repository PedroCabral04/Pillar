using erp.Models.Financial;

namespace erp.DTOs.Financial;

/// <summary>
/// DTO for creating an account payable
/// </summary>
public class CreateAccountPayableDto
{
    public int SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal InterestAmount { get; set; } = 0;
    public decimal FineAmount { get; set; } = 0;
    
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankSlip;
    public string? BankSlipNumber { get; set; }
    public string? PixKey { get; set; }
    
    public bool RequiresApproval { get; set; } = false;
    
    public int? CategoryId { get; set; }
    public int? CostCenterId { get; set; }
    
    public int? ParentAccountId { get; set; }
    public int InstallmentNumber { get; set; } = 1;
    public int TotalInstallments { get; set; } = 1;
    
    public string? InvoiceAttachmentUrl { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
}

/// <summary>
/// DTO for updating an account payable
/// </summary>
public class UpdateAccountPayableDto : CreateAccountPayableDto
{
    public AccountStatus Status { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public DateTime? PaymentDate { get; set; }
    public string? ProofOfPaymentUrl { get; set; }
}

/// <summary>
/// DTO for account payable response
/// </summary>
public class AccountPayableDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? InvoiceNumber { get; set; }
    
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FineAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal RemainingAmount => NetAmount - PaidAmount;
    
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    
    public AccountStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodDescription { get; set; } = string.Empty;
    
    public string? BankSlipNumber { get; set; }
    public string? PixKey { get; set; }
    
    public bool RequiresApproval { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalNotes { get; set; }
    
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    
    public int? ParentAccountId { get; set; }
    public int InstallmentNumber { get; set; }
    public int TotalInstallments { get; set; }
    
    public string? InvoiceAttachmentUrl { get; set; }
    public string? ProofOfPaymentUrl { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public int? PaidByUserId { get; set; }
    public string? PaidByUserName { get; set; }
    
    public int DaysOverdue
    {
        get
        {
            if (Status == AccountStatus.Paid || DateTime.UtcNow < DueDate)
                return 0;
            return (DateTime.UtcNow - DueDate).Days;
        }
    }
}

/// <summary>
/// DTO for approving an account payable
/// </summary>
public class ApproveAccountPayableDto
{
    public bool Approved { get; set; }
    public string? ApprovalNotes { get; set; }
}

/// <summary>
/// DTO for paying an account payable
/// </summary>
public class PayAccountPayableDto
{
    public decimal PaidAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal? AdditionalDiscount { get; set; }
    public decimal? AdditionalInterest { get; set; }
    public decimal? AdditionalFine { get; set; }
    public string? ProofOfPaymentUrl { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for account payable summary (lists)
/// </summary>
public class AccountPayableSummaryDto
{
    public int Id { get; set; }
    public string? SupplierName { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => NetAmount - PaidAmount;
    public DateTime DueDate { get; set; }
    public AccountStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public bool IsApproved { get; set; }
    public int DaysOverdue { get; set; }
}
