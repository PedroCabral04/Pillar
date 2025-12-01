using erp.Models.Financial;

namespace erp.Extensions;

/// <summary>
/// Extension methods para tradução de enums para português
/// </summary>
public static class EnumDisplayExtensions
{
    /// <summary>
    /// Retorna o nome em português do método de pagamento
    /// </summary>
    public static string ToDisplayName(this PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => "Dinheiro",
            PaymentMethod.BankSlip => "Boleto Bancário",
            PaymentMethod.Pix => "PIX",
            PaymentMethod.CreditCard => "Cartão de Crédito",
            PaymentMethod.DebitCard => "Cartão de Débito",
            PaymentMethod.BankTransfer => "Transferência Bancária",
            PaymentMethod.Check => "Cheque",
            PaymentMethod.Other => "Outro",
            _ => paymentMethod.ToString()
        };
    }

    /// <summary>
    /// Retorna o ícone Material Design correspondente ao método de pagamento
    /// </summary>
    public static string ToIcon(this PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => "AttachMoney",
            PaymentMethod.BankSlip => "Receipt",
            PaymentMethod.Pix => "QrCode2",
            PaymentMethod.CreditCard => "CreditCard",
            PaymentMethod.DebitCard => "CreditCard",
            PaymentMethod.BankTransfer => "AccountBalance",
            PaymentMethod.Check => "MonetizationOn",
            PaymentMethod.Other => "MoreHoriz",
            _ => "Payment"
        };
    }

    /// <summary>
    /// Retorna o nome em português do status da conta
    /// </summary>
    public static string ToDisplayName(this AccountStatus status)
    {
        return status switch
        {
            AccountStatus.Pending => "Pendente",
            AccountStatus.Paid => "Pago",
            AccountStatus.Overdue => "Vencido",
            AccountStatus.Cancelled => "Cancelado",
            AccountStatus.PartiallyPaid => "Parcialmente Pago",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Retorna a cor MudBlazor correspondente ao status da conta
    /// </summary>
    public static string ToColorClass(this AccountStatus status)
    {
        return status switch
        {
            AccountStatus.Pending => "warning",
            AccountStatus.Paid => "success",
            AccountStatus.Overdue => "error",
            AccountStatus.Cancelled => "default",
            AccountStatus.PartiallyPaid => "info",
            _ => "default"
        };
    }
}
