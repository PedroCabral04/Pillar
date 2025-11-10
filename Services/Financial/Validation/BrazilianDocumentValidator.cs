using System.Text.RegularExpressions;

namespace erp.Services.Financial.Validation;

/// <summary>
/// Validator for Brazilian documents (CPF and CNPJ)
/// </summary>
public static class BrazilianDocumentValidator
{
    /// <summary>
    /// Validates a CPF (Cadastro de Pessoas Físicas)
    /// </summary>
    public static bool IsValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // Remove non-numeric characters
        cpf = Regex.Replace(cpf, @"[^\d]", "");

        // CPF must have 11 digits
        if (cpf.Length != 11)
            return false;

        // Check for known invalid CPFs (all same digits)
        if (cpf.Distinct().Count() == 1)
            return false;

        // Validate first check digit
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        int remainder = sum % 11;
        int firstCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cpf[9].ToString()) != firstCheckDigit)
            return false;

        // Validate second check digit
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        remainder = sum % 11;
        int secondCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cpf[10].ToString()) == secondCheckDigit;
    }

    /// <summary>
    /// Validates a CNPJ (Cadastro Nacional da Pessoa Jurídica)
    /// </summary>
    public static bool IsValidCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        // Remove non-numeric characters
        cnpj = Regex.Replace(cnpj, @"[^\d]", "");

        // CNPJ must have 14 digits
        if (cnpj.Length != 14)
            return false;

        // Check for known invalid CNPJs (all same digits)
        if (cnpj.Distinct().Count() == 1)
            return false;

        // Validate first check digit
        int[] multiplier1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = 0;

        for (int i = 0; i < 12; i++)
            sum += int.Parse(cnpj[i].ToString()) * multiplier1[i];

        int remainder = sum % 11;
        int firstCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cnpj[12].ToString()) != firstCheckDigit)
            return false;

        // Validate second check digit
        int[] multiplier2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = 0;

        for (int i = 0; i < 13; i++)
            sum += int.Parse(cnpj[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        int secondCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cnpj[13].ToString()) == secondCheckDigit;
    }

    /// <summary>
    /// Validates CPF or CNPJ based on length
    /// </summary>
    public static bool IsValidDocument(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return false;

        var cleaned = Regex.Replace(document, @"[^\d]", "");

        return cleaned.Length switch
        {
            11 => IsValidCpf(document),
            14 => IsValidCnpj(document),
            _ => false
        };
    }

    /// <summary>
    /// Formats a CPF with standard mask (000.000.000-00)
    /// </summary>
    public static string FormatCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return cpf;

        cpf = Regex.Replace(cpf, @"[^\d]", "");

        if (cpf.Length != 11)
            return cpf;

        return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
    }

    /// <summary>
    /// Formats a CNPJ with standard mask (00.000.000/0000-00)
    /// </summary>
    public static string FormatCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return cnpj;

        cnpj = Regex.Replace(cnpj, @"[^\d]", "");

        if (cnpj.Length != 14)
            return cnpj;

        return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    /// <summary>
    /// Removes formatting from document
    /// </summary>
    public static string RemoveFormatting(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return document;

        return Regex.Replace(document, @"[^\d]", "");
    }
}
