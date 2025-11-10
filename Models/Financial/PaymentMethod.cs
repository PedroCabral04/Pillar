namespace erp.Models.Financial;

/// <summary>
/// Payment methods for financial transactions
/// </summary>
public enum PaymentMethod
{
    Cash = 0,
    BankSlip = 1,     // Boleto
    Pix = 2,
    CreditCard = 3,
    DebitCard = 4,
    BankTransfer = 5,
    Check = 6,
    Other = 99
}
