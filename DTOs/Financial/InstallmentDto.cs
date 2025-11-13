namespace erp.DTOs.Financial;

/// <summary>
/// DTO for creating installments from a base account
/// Shared between AccountReceivable and AccountPayable
/// </summary>
public class CreateInstallmentsDto
{
    public int NumberOfInstallments { get; set; }
    public decimal InterestRate { get; set; } = 0;
    public string InstallmentMethod { get; set; } = "PRICE"; // PRICE or SAC
}
