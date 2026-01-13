using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models.Sales;
using erp.Models;

namespace erp.Models.Financial;

/// <summary>
/// Represents an account receivable (conta a receber)
/// </summary>
public class AccountReceivable : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    // Relacionamentos
    public int CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }
    
    // Dados Financeiros
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; } // Número da NF/Fatura
    
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
    /// Saldo restante a receber: NetAmount - PaidAmount
    /// </summary>
    public decimal RemainingAmount => NetAmount - PaidAmount;
    
    // Datas
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    
    // Status e Controle
    public AccountStatus Status { get; set; } = AccountStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    
    [MaxLength(100)]
    public string? BankSlipNumber { get; set; } // Número do boleto
    
    [MaxLength(100)]
    public string? PixKey { get; set; }
    
    // Categorização
    public int? CategoryId { get; set; }
    public virtual FinancialCategory? Category { get; set; }
    
    public int? CostCenterId { get; set; }
    public virtual CostCenter? CostCenter { get; set; }
    
    // Parcelamento
    public int? ParentAccountId { get; set; }
    public virtual AccountReceivable? ParentAccount { get; set; }
    public int InstallmentNumber { get; set; } = 1;
    public int TotalInstallments { get; set; } = 1;
    
    // Observações
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; } // Não visível para cliente
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
    
    public int? ReceivedByUserId { get; set; }
    public virtual ApplicationUser? ReceivedByUser { get; set; }
    
    // Relacionamento com parcelas filhas
    public virtual ICollection<AccountReceivable> Installments { get; set; } = new List<AccountReceivable>();
}
