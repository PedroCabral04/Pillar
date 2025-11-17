namespace erp.Models.Payroll;

/// <summary>
/// Classifica se um componente da folha representa proventos, descontos ou encargos.
/// </summary>
public enum PayrollComponentType
{
    Earning = 0,
    Deduction = 1,
    Contribution = 2
}

/// <summary>
/// Tipos de tabelas tributárias suportadas pela folha.
/// </summary>
public enum PayrollTaxType
{
    Inss = 0,
    Irrf = 1
}
