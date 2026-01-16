namespace erp.Models.Payroll;

/// <summary>
/// Modo de cálculo da folha de pagamento.
/// </summary>
public enum PayrollCalculationMode
{
    /// <summary>
    /// Cálculo simplificado: sem INSS nem IRRF.
    /// </summary>
    Simplified = 0,

    /// <summary>
    /// Cálculo completo: com todos os descontos (INSS, IRRF).
    /// </summary>
    Full = 1
}
