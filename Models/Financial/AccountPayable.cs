using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models;

namespace erp.Models.Financial;

/// <summary>
/// Represents an account payable (conta a pagar)
/// </summary>
public class AccountPayable : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    // Relacionamentos
    public int SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }
    
    // Dados Financeiros
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; } // Número da NF
    
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal InterestAmount { get; set; } = 0;
    public decimal FineAmount { get; set; } = 0;
    public decimal PaidAmount { get; set; } = 0;
    
    /// <summary>
    /// Valor líquido: Original - Desconto + Juros + Multa
    /// </summary>
    public decimal NetAmount => OriginalAmount - DiscountAmount + InterestAmount + FineAmount;
    
    /// <summary>
    /// Saldo restante a pagar: NetAmount - PaidAmount
    /// </summary>
    public decimal RemainingAmount => NetAmount - PaidAmount;
    
    // Datas
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    
    // Status e Controle
    public AccountStatus Status { get; set; } = AccountStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankSlip;
    
    [MaxLength(100)]
    public string? BankSlipNumber { get; set; }
    
    [MaxLength(100)]
    public string? PixKey { get; set; }
    
    // Workflow de Aprovação
    public bool RequiresApproval { get; set; } = false;
    public int? ApprovedByUserId { get; set; }
    public virtual ApplicationUser? ApprovedByUser { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalNotes { get; set; }
    
    // Categorização
    public int? CategoryId { get; set; }
    public virtual FinancialCategory? Category { get; set; }
    
    public int? CostCenterId { get; set; }
    public virtual CostCenter? CostCenter { get; set; }
    
    // Parcelamento
    public int? ParentAccountId { get; set; }
    public virtual AccountPayable? ParentAccount { get; set; }
    public int InstallmentNumber { get; set; } = 1;
    public int TotalInstallments { get; set; } = 1;
    
    // Documentos
    [MaxLength(500)]
    public string? InvoiceAttachmentUrl { get; set; }
    
    [MaxLength(500)]
    public string? ProofOfPaymentUrl { get; set; }
    
    // Observações
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
    
    public int? PaidByUserId { get; set; }
    public virtual ApplicationUser? PaidByUser { get; set; }
    
    // Relacionamento com parcelas filhas
    public virtual ICollection<AccountPayable> Installments { get; set; } = new List<AccountPayable>();
}
