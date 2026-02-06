using erp.Models.Financial;

namespace erp.Services.Financial;

public static class PaymentMethodResolver
{
    public static PaymentMethod FromSaleText(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return PaymentMethod.Other;
        }

        var normalized = method.Trim().ToUpperInvariant();

        return normalized switch
        {
            "DINHEIRO" => PaymentMethod.Cash,
            "PIX" => PaymentMethod.Pix,
            "BOLETO" or "BOLETO BANCARIO" => PaymentMethod.BankSlip,
            "DEBITO" or "DÉBITO" or "CARTAO DE DEBITO" or "CARTÃO DE DÉBITO" => PaymentMethod.DebitCard,
            "CREDITO" or "CRÉDITO" or "CARTAO DE CREDITO" or "CARTÃO DE CRÉDITO" => PaymentMethod.CreditCard,
            "TRANSFERENCIA" or "TRANSFERÊNCIA" or "TRANSFERENCIA BANCARIA" or "TRANSFERÊNCIA BANCÁRIA" => PaymentMethod.BankTransfer,
            "CHEQUE" => PaymentMethod.Check,
            _ => PaymentMethod.Other
        };
    }

    public static bool IsImmediatelyPaid(PaymentMethod method)
    {
        return method is PaymentMethod.Cash or PaymentMethod.Pix or PaymentMethod.CreditCard or PaymentMethod.DebitCard or PaymentMethod.BankTransfer;
    }
}
