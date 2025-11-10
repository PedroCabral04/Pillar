using erp.Models.Financial;

namespace erp.Services.Financial;

/// <summary>
/// Service for financial calculations (interest, fines, installments)
/// </summary>
public interface IAccountingService
{
    /// <summary>
    /// Calculate interest based on days overdue
    /// </summary>
    decimal CalculateInterest(decimal amount, int daysOverdue, decimal dailyRate = 0.00033m);
    
    /// <summary>
    /// Calculate fine for late payment
    /// </summary>
    decimal CalculateFine(decimal amount, decimal fineRate = 0.02m);
    
    /// <summary>
    /// Calculate total charges for overdue account
    /// </summary>
    (decimal Interest, decimal Fine) CalculateOverdueCharges(
        decimal amount, 
        DateTime dueDate, 
        decimal dailyInterestRate = 0.00033m, 
        decimal fineRate = 0.02m);
    
    /// <summary>
    /// Calculate installment values using Price table (equal installments)
    /// </summary>
    List<InstallmentCalculation> CalculateInstallments(
        decimal totalAmount, 
        int installments, 
        decimal monthlyInterestRate = 0);
    
    /// <summary>
    /// Calculate installment values using SAC (Sistema de Amortização Constante)
    /// </summary>
    List<InstallmentCalculation> CalculateInstallmentsSAC(
        decimal totalAmount, 
        int installments, 
        decimal monthlyInterestRate);
    
    /// <summary>
    /// Update account status based on payment and due date
    /// </summary>
    AccountStatus DetermineAccountStatus(
        decimal totalAmount, 
        decimal paidAmount, 
        DateTime dueDate);
}

public class AccountingService : IAccountingService
{
    public decimal CalculateInterest(decimal amount, int daysOverdue, decimal dailyRate = 0.00033m)
    {
        if (daysOverdue <= 0)
            return 0;

        return Math.Round(amount * dailyRate * daysOverdue, 2);
    }

    public decimal CalculateFine(decimal amount, decimal fineRate = 0.02m)
    {
        return Math.Round(amount * fineRate, 2);
    }

    public (decimal Interest, decimal Fine) CalculateOverdueCharges(
        decimal amount, 
        DateTime dueDate, 
        decimal dailyInterestRate = 0.00033m, 
        decimal fineRate = 0.02m)
    {
        var daysOverdue = (DateTime.UtcNow - dueDate).Days;
        
        if (daysOverdue <= 0)
            return (0, 0);

        var interest = CalculateInterest(amount, daysOverdue, dailyInterestRate);
        var fine = CalculateFine(amount, fineRate);

        return (interest, fine);
    }

    public List<InstallmentCalculation> CalculateInstallments(
        decimal totalAmount, 
        int installments, 
        decimal monthlyInterestRate = 0)
    {
        var result = new List<InstallmentCalculation>();

        if (monthlyInterestRate == 0)
        {
            // Without interest - equal installments
            var installmentValue = Math.Round(totalAmount / installments, 2);
            var lastInstallmentAdjustment = totalAmount - (installmentValue * (installments - 1));

            for (int i = 1; i <= installments; i++)
            {
                result.Add(new InstallmentCalculation
                {
                    InstallmentNumber = i,
                    PrincipalAmount = i == installments ? lastInstallmentAdjustment : installmentValue,
                    InterestAmount = 0,
                    TotalAmount = i == installments ? lastInstallmentAdjustment : installmentValue
                });
            }
        }
        else
        {
            // Price table - with interest
            var rate = monthlyInterestRate / 100;
            var rateDouble = (double)rate;
            var installmentValue = totalAmount * (rate * (decimal)Math.Pow(1 + rateDouble, installments)) / 
                                   ((decimal)Math.Pow(1 + rateDouble, installments) - 1);
            installmentValue = Math.Round(installmentValue, 2);

            var remainingBalance = totalAmount;

            for (int i = 1; i <= installments; i++)
            {
                var interestAmount = Math.Round(remainingBalance * rate, 2);
                var principalAmount = installmentValue - interestAmount;

                if (i == installments)
                {
                    // Adjust last installment
                    principalAmount = remainingBalance;
                    installmentValue = principalAmount + interestAmount;
                }

                result.Add(new InstallmentCalculation
                {
                    InstallmentNumber = i,
                    PrincipalAmount = principalAmount,
                    InterestAmount = interestAmount,
                    TotalAmount = installmentValue
                });

                remainingBalance -= principalAmount;
            }
        }

        return result;
    }

    public List<InstallmentCalculation> CalculateInstallmentsSAC(
        decimal totalAmount, 
        int installments, 
        decimal monthlyInterestRate)
    {
        var result = new List<InstallmentCalculation>();
        var rate = monthlyInterestRate / 100;
        var principalPerInstallment = Math.Round(totalAmount / installments, 2);
        var remainingBalance = totalAmount;

        for (int i = 1; i <= installments; i++)
        {
            var interestAmount = Math.Round(remainingBalance * rate, 2);
            var principalAmount = i == installments ? remainingBalance : principalPerInstallment;
            var totalInstallment = principalAmount + interestAmount;

            result.Add(new InstallmentCalculation
            {
                InstallmentNumber = i,
                PrincipalAmount = principalAmount,
                InterestAmount = interestAmount,
                TotalAmount = totalInstallment
            });

            remainingBalance -= principalAmount;
        }

        return result;
    }

    public AccountStatus DetermineAccountStatus(
        decimal totalAmount, 
        decimal paidAmount, 
        DateTime dueDate)
    {
        if (paidAmount >= totalAmount)
            return AccountStatus.Paid;

        if (paidAmount > 0)
            return AccountStatus.PartiallyPaid;

        if (DateTime.UtcNow > dueDate)
            return AccountStatus.Overdue;

        return AccountStatus.Pending;
    }
}

public class InstallmentCalculation
{
    public int InstallmentNumber { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
