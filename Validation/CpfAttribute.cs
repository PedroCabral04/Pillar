using System.ComponentModel.DataAnnotations;

namespace erp.Validation;

/// <summary>
/// Valida o número de CPF (Cadastro de Pessoas Físicas) brasileiro
/// Implementa validação de formato e dígitos verificadores
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class CpfAttribute : ValidationAttribute
{
    public CpfAttribute()
    {
        ErrorMessage = "CPF inválido.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Se o valor for nulo ou vazio, considera válido (campo opcional)
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var cpf = value.ToString()!;

        // Remove caracteres não numéricos
        var numbersOnly = new string(cpf.Where(char.IsDigit).ToArray());

        // Validações básicas
        if (numbersOnly.Length != 11)
        {
            return new ValidationResult(ErrorMessage);
        }

        // Verifica se todos os dígitos são iguais (CPF inválido mas passa no algoritmo)
        if (numbersOnly.Distinct().Count() == 1)
        {
            return new ValidationResult(ErrorMessage);
        }

        // Valida dígitos verificadores
        if (!IsValidCpf(numbersOnly))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }

    private static bool IsValidCpf(string cpf)
    {
        // Calcula o primeiro dígito verificador
        var sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (10 - i) * (cpf[i] - '0');
        }

        var remainder = sum % 11;
        var firstDigit = remainder < 2 ? 0 : 11 - remainder;

        if (firstDigit != (cpf[9] - '0'))
        {
            return false;
        }

        // Calcula o segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += (11 - i) * (cpf[i] - '0');
        }

        remainder = sum % 11;
        var secondDigit = remainder < 2 ? 0 : 11 - remainder;

        return secondDigit == (cpf[10] - '0');
    }
}

/// <summary>
/// Extensão para formatar CPF para exibição
/// </summary>
public static class CpfFormatter
{
    /// <summary>
    /// Formata CPF para exibição (000.000.000-00)
    /// </summary>
    public static string FormatCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        var numbersOnly = new string(cpf.Where(char.IsDigit).ToArray());

        if (numbersOnly.Length != 11)
            return cpf;

        return Convert.ToUInt64(numbersOnly).ToString(@"000\.000\.000\-00");
    }

    /// <summary>
    /// Limpa formatação do CPF, deixando apenas números
    /// </summary>
    public static string CleanCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
}
