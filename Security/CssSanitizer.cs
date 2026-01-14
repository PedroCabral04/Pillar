using System.Text.RegularExpressions;

namespace erp.Security;

/// <summary>
/// Sanitizes CSS to prevent XSS attacks through custom CSS injection.
/// Only allows safe CSS properties (colors, fonts, spacing) and blocks dangerous patterns.
/// </summary>
public static partial class CssSanitizer
{
    /// <summary>
    /// Patterns that are absolutely forbidden in CSS (XSS vectors).
    /// </summary>
    private static readonly string[] ForbiddenPatterns =
    [
        "url(",         // Can inject javascript: or data: URLs
        "@import",      // Can import external malicious resources
        "@charset",     // Can be used for encoding attacks
        "@font-face",   // Can load external fonts with potential exploits
        "expression(",  // IE-only CSS expression (JavaScript execution)
        "javascript:",  // JavaScript protocol
        "data:",        // Data URLs (could contain scripts in some contexts)
        "vbscript:",    // VBScript protocol
        "<script",      // Script tags in CSS
        "behavior:",    // IE behavior (can execute HTC files)
        "-webkit-binding", // Chrome/XSS CSS binding
    ];

    /// <summary>
    /// Allowed CSS properties (whitelist approach).
    /// These are safe properties that only control visual appearance.
    /// </summary>
    private static readonly string[] AllowedProperties =
    [
        // Colors
        "color", "background-color", "border-color", "outline-color",
        "opacity",
        // Fonts
        "font-family", "font-size", "font-weight", "font-style",
        "font-variant", "line-height", "letter-spacing", "word-spacing",
        "text-align", "text-decoration", "text-transform",
        // Spacing
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
        "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
        "gap", "row-gap", "column-gap",
        // Borders (non-image)
        "border", "border-width", "border-style",
        "border-top", "border-right", "border-bottom", "border-left",
        "border-radius", "border-top-left-radius", "border-top-right-radius",
        "border-bottom-left-radius", "border-bottom-right-radius",
        // Sizing
        "width", "height", "max-width", "max-height", "min-width", "min-height",
        // Display
        "display", "visibility", "overflow", "position", "z-index",
        "top", "right", "bottom", "left",
        // Other safe visual properties
        "cursor", "box-shadow", "text-shadow", "transform", "transition",
        "flex-direction", "flex-wrap", "justify-content", "align-items",
        "grid-template-columns", "grid-template-rows"
    ];

    /// <summary>
    /// Sanitizes CSS input by removing dangerous patterns and non-whitelisted properties.
    /// </summary>
    /// <param name="css">The CSS to sanitize.</param>
    /// <returns>Sanitized CSS containing only safe properties, or empty string if input is null/empty.</returns>
    public static string Sanitize(string? css)
    {
        if (string.IsNullOrWhiteSpace(css))
            return string.Empty;

        // Check for forbidden patterns first
        var lowerCss = css.ToLowerInvariant();
        foreach (var pattern in ForbiddenPatterns)
        {
            if (lowerCss.Contains(pattern.ToLowerInvariant()))
            {
                // Return empty string if any forbidden pattern is found
                return string.Empty;
            }
        }

        // Parse and filter CSS rules
        var sanitized = SanitizeCssProperties(css);

        return sanitized;
    }

    /// <summary>
    /// Parses CSS and keeps only allowed properties.
    /// </summary>
    private static string SanitizeCssProperties(string css)
    {
        // Simple CSS parser: find property: value; declarations
        var lines = css.Split([';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var validDeclarations = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Split by first colon to get property and value
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var property = trimmed.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = trimmed.Substring(colonIndex + 1).Trim();

            // Check if property is allowed
            if (AllowedProperties.Contains(property))
            {
                // Additional validation for the value
                if (IsValueSafe(value))
                {
                    validDeclarations.Add($"{property}: {value};");
                }
            }
        }

        return string.Join(" ", validDeclarations);
    }

    /// <summary>
    /// Allowed CSS functions that are safe to use.
    /// </summary>
    private static readonly string[] AllowedFunctions =
    [
        "rgb(", "rgba(", "hsl(", "hsla(",
        "var(",        // CSS custom properties (only --variables, not arbitrary content)
        "calc(",       // CSS calculations
        "min(", "max(", "clamp(",
        "linear-gradient(", "radial-gradient(",
        "translateX(", "translateY(", "translate(",
        "rotate(", "scale(", "skew(",
    ];

    /// <summary>
    /// Validates that a CSS value doesn't contain dangerous content.
    /// </summary>
    private static bool IsValueSafe(string value)
    {
        var lowerValue = value.ToLowerInvariant();

        // 1. Check for non-function forbidden patterns (scripts, expressions, etc.)
        // Function-like patterns (ending with '(') are skipped here because they are
        // checked more accurately in the whitelisted functions step below.
        foreach (var pattern in ForbiddenPatterns)
        {
            if (!pattern.EndsWith('(') && lowerValue.Contains(pattern))
                return false;
        }

        // 2. Check for function calls - only allow whitelisted functions
        if (lowerValue.Contains('('))
        {
            var functions = ExtractFunctions(lowerValue);
            if (functions.Count == 0 && lowerValue.Contains('('))
            {
                // Has parentheses but no valid function name matches - could be an obfuscation attempt
                return false;
            }

            foreach (var func in functions)
            {
                if (!AllowedFunctions.Any(allowed => func.StartsWith(allowed)))
                {
                    return false;
                }
            }
        }

        // 3. Validate var() usage - all var() calls must follow the proper format
        if (lowerValue.Contains("var("))
        {
            var varMatches = VarPropertyRegex().Matches(lowerValue);
            var varCalls = ExtractFunctions(lowerValue).Count(f => f == "var(");
            
            if (varMatches.Count != varCalls)
                return false; // Some var() calls don't match the allowed --property format
        }

        // 4. Specific rejections for safe-looking but dangerous patterns
        if (lowerValue.Contains("attr(")) // Can be used to inject attributes
            return false;

        if (value.Contains('\\')) // Escape character bypass
            return false;

        if (value.Contains('<') || value.Contains('>')) // HTML injection
            return false;

        return true;
    }

    /// <summary>
    /// Extracts all function calls from a CSS value.
    /// </summary>
    private static List<string> ExtractFunctions(string value)
    {
        var functions = new List<string>();
        var regex = FunctionRegex();
        var matches = regex.Matches(value);
        foreach (Match match in matches)
        {
            functions.Add(match.Value);
        }
        return functions;
    }

    [GeneratedRegex(@"[a-z\-]+\(", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionRegex();

    [GeneratedRegex(@"var\(\s*--[a-z0-9\-_]+\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex VarPropertyRegex();
}
