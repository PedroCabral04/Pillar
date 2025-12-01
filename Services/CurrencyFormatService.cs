using System.Globalization;
using erp.DTOs.Preferences;

namespace erp.Services
{
    /// <summary>
    /// Provides currency formatting based on user preferences.
    /// Inject this service in Blazor components to format currency values dynamically.
    /// Use static methods for backend services without user context.
    /// </summary>
    public class CurrencyFormatService
    {
        private static readonly CultureInfo PtBrCulture;
        private readonly PreferenceService _preferenceService;

        static CurrencyFormatService()
        {
            // Initialize static pt-BR culture for backend services
            PtBrCulture = (CultureInfo)CultureInfo.GetCultureInfo("pt-BR").Clone();
            PtBrCulture.NumberFormat.CurrencySymbol = "R$";
        }
        private CultureInfo? _cachedCulture;
        private string? _cachedCurrency;
        private string? _cachedNumberFormat;

        public CurrencyFormatService(PreferenceService preferenceService)
        {
            _preferenceService = preferenceService;
            _preferenceService.OnPreferenceChanged += InvalidateCache;
        }

        private void InvalidateCache()
        {
            _cachedCulture = null;
        }

        /// <summary>
        /// Gets a CultureInfo configured with user's currency and number format preferences.
        /// </summary>
        public CultureInfo GetCurrencyCulture()
        {
            var prefs = _preferenceService.CurrentPreferences.Locale;
            
            // Check if cache is valid
            if (_cachedCulture != null && 
                _cachedCurrency == prefs.Currency && 
                _cachedNumberFormat == prefs.NumberFormat)
            {
                return _cachedCulture;
            }

            // Create a new culture based on user preferences
            var baseCulture = prefs.Language ?? "pt-BR";
            var culture = (CultureInfo)CultureInfo.GetCultureInfo(baseCulture).Clone();

            // Apply number format
            if (prefs.NumberFormat == "1.234,56")
            {
                culture.NumberFormat.NumberGroupSeparator = ".";
                culture.NumberFormat.NumberDecimalSeparator = ",";
                culture.NumberFormat.CurrencyGroupSeparator = ".";
                culture.NumberFormat.CurrencyDecimalSeparator = ",";
            }
            else if (prefs.NumberFormat == "1,234.56")
            {
                culture.NumberFormat.NumberGroupSeparator = ",";
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.NumberFormat.CurrencyGroupSeparator = ",";
                culture.NumberFormat.CurrencyDecimalSeparator = ".";
            }

            // Apply currency symbol
            culture.NumberFormat.CurrencySymbol = prefs.Currency switch
            {
                "BRL" => "R$",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                "JPY" => "¥",
                _ => !string.IsNullOrEmpty(prefs.Currency) ? prefs.Currency : "R$"
            };

            // Cache the result
            _cachedCulture = culture;
            _cachedCurrency = prefs.Currency;
            _cachedNumberFormat = prefs.NumberFormat;

            return culture;
        }

        /// <summary>
        /// Formats a decimal value as currency using user preferences.
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <param name="decimals">Number of decimal places (default: 2)</param>
        /// <returns>Formatted currency string</returns>
        public string Format(decimal value, int decimals = 2)
        {
            return value.ToString($"C{decimals}", GetCurrencyCulture());
        }

        /// <summary>
        /// Formats a nullable decimal value as currency using user preferences.
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <param name="decimals">Number of decimal places (default: 2)</param>
        /// <param name="nullValue">Value to return if null (default: "-")</param>
        /// <returns>Formatted currency string or nullValue</returns>
        public string Format(decimal? value, int decimals = 2, string nullValue = "-")
        {
            return value.HasValue ? Format(value.Value, decimals) : nullValue;
        }

        /// <summary>
        /// Gets the currency symbol based on user preferences.
        /// </summary>
        public string CurrencySymbol => GetCurrencyCulture().NumberFormat.CurrencySymbol;

        /// <summary>
        /// Static method for backend services to format currency using pt-BR culture.
        /// Use this when user preferences are not available (e.g., in background services).
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <param name="decimals">Number of decimal places (default: 2)</param>
        /// <returns>Formatted currency string</returns>
        public static string FormatStatic(decimal value, int decimals = 2)
        {
            return value.ToString($"C{decimals}", PtBrCulture);
        }

        /// <summary>
        /// Static method for backend services to format nullable currency using pt-BR culture.
        /// </summary>
        public static string FormatStatic(decimal? value, int decimals = 2, string nullValue = "-")
        {
            return value.HasValue ? FormatStatic(value.Value, decimals) : nullValue;
        }
    }
}
