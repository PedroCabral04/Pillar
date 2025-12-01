namespace erp.Extensions;

/// <summary>
/// Extension methods para formatação padronizada de datas no sistema
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Formato padrão brasileiro de data: dd/MM/yyyy
    /// </summary>
    public const string DateFormat = "dd/MM/yyyy";
    
    /// <summary>
    /// Formato padrão brasileiro de data e hora: dd/MM/yyyy HH:mm
    /// </summary>
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    
    /// <summary>
    /// Formato padrão brasileiro de data e hora com segundos: dd/MM/yyyy HH:mm:ss
    /// </summary>
    public const string DateTimeSecondsFormat = "dd/MM/yyyy HH:mm:ss";
    
    /// <summary>
    /// Formato extenso de data: segunda-feira, 01 de janeiro de 2025
    /// </summary>
    public const string LongDateFormat = "dddd, dd 'de' MMMM 'de' yyyy";

    /// <summary>
    /// Formata a data no padrão brasileiro (dd/MM/yyyy)
    /// </summary>
    public static string ToBrazilianDate(this DateTime date)
    {
        return date.ToString(DateFormat);
    }

    /// <summary>
    /// Formata a data nullable no padrão brasileiro (dd/MM/yyyy)
    /// </summary>
    public static string ToBrazilianDate(this DateTime? date, string defaultValue = "-")
    {
        return date?.ToString(DateFormat) ?? defaultValue;
    }

    /// <summary>
    /// Formata a data e hora no padrão brasileiro (dd/MM/yyyy HH:mm)
    /// </summary>
    public static string ToBrazilianDateTime(this DateTime date)
    {
        return date.ToString(DateTimeFormat);
    }

    /// <summary>
    /// Formata a data e hora nullable no padrão brasileiro (dd/MM/yyyy HH:mm)
    /// </summary>
    public static string ToBrazilianDateTime(this DateTime? date, string defaultValue = "-")
    {
        return date?.ToString(DateTimeFormat) ?? defaultValue;
    }

    /// <summary>
    /// Formata a data e hora com segundos no padrão brasileiro (dd/MM/yyyy HH:mm:ss)
    /// </summary>
    public static string ToBrazilianDateTimeSeconds(this DateTime date)
    {
        return date.ToString(DateTimeSecondsFormat);
    }

    /// <summary>
    /// Formata a data e hora com segundos nullable no padrão brasileiro (dd/MM/yyyy HH:mm:ss)
    /// </summary>
    public static string ToBrazilianDateTimeSeconds(this DateTime? date, string defaultValue = "-")
    {
        return date?.ToString(DateTimeSecondsFormat) ?? defaultValue;
    }

    /// <summary>
    /// Formata a data no formato extenso brasileiro (segunda-feira, 01 de janeiro de 2025)
    /// </summary>
    public static string ToBrazilianLongDate(this DateTime date)
    {
        return date.ToString(LongDateFormat, new System.Globalization.CultureInfo("pt-BR"));
    }

    /// <summary>
    /// Formata a data nullable no formato extenso brasileiro
    /// </summary>
    public static string ToBrazilianLongDate(this DateTime? date, string defaultValue = "-")
    {
        return date?.ToString(LongDateFormat, new System.Globalization.CultureInfo("pt-BR")) ?? defaultValue;
    }

    /// <summary>
    /// Retorna uma representação relativa da data (Hoje, Ontem, há X dias, etc.)
    /// </summary>
    public static string ToRelativeDate(this DateTime date)
    {
        var today = DateTime.Today;
        var diff = (today - date.Date).Days;

        return diff switch
        {
            0 => "Hoje",
            1 => "Ontem",
            -1 => "Amanhã",
            > 1 and <= 7 => $"Há {diff} dias",
            < -1 and >= -7 => $"Em {-diff} dias",
            > 7 and <= 30 => $"Há {diff / 7} semana(s)",
            < -7 and >= -30 => $"Em {-diff / 7} semana(s)",
            _ => date.ToBrazilianDate()
        };
    }

    /// <summary>
    /// Retorna uma representação relativa da data nullable
    /// </summary>
    public static string ToRelativeDate(this DateTime? date, string defaultValue = "-")
    {
        return date?.ToRelativeDate() ?? defaultValue;
    }
}
