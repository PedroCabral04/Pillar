namespace erp.Models.Sales;

/// <summary>
/// Constantes de status para vendas
/// </summary>
public static class SaleStatus
{
    public const string Pending = "Pendente";
    public const string Finalized = "Finalizada";
    public const string Cancelled = "Cancelada";

    public static readonly string[] All = { Pending, Finalized, Cancelled };

    /// <summary>
    /// Verifica se o status fornecido é válido
    /// </summary>
    public static bool IsValid(string status)
    {
        return All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se a venda está finalizada
    /// </summary>
    public static bool IsFinalized(string status)
    {
        return string.Equals(status, Finalized, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se a venda está cancelada
    /// </summary>
    public static bool IsCancelled(string status)
    {
        return string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se a venda está pendente
    /// </summary>
    public static bool IsPending(string status)
    {
        return string.Equals(status, Pending, StringComparison.OrdinalIgnoreCase);
    }
}
