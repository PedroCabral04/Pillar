namespace erp.Models.Financial;

/// <summary>
/// Status for accounts receivable and payable
/// </summary>
public enum AccountStatus
{
    Pending = 0,      // Pendente
    Overdue = 1,      // Vencido
    Paid = 2,         // Pago
    PartiallyPaid = 3,// Parcialmente pago
    Cancelled = 4     // Cancelado
}
