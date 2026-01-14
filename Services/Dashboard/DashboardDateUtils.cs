namespace erp.Services.Dashboard;

/// <summary>
/// Utility class for common date formatting operations in dashboard providers.
/// </summary>
public static class DashboardDateUtils
{
    /// <summary>
    /// Formats a date range as a human-readable period label.
    /// </summary>
    /// <param name="from">Start date (defaults to today if null)</param>
    /// <param name="to">End date (defaults to today if null)</param>
    /// <returns>Formatted period string (e.g., "01/01/2024 - 31/01/2024" or "01/01/2024" if same day)</returns>
    public static string FormatPeriodLabel(DateTime? from, DateTime? to)
    {
        var start = from?.Date ?? DateTime.Today;
        var end = to?.Date ?? DateTime.Today;
        
        if (start == end)
            return start.ToString("dd/MM/yyyy");
            
        return $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";
    }
    
    /// <summary>
    /// Checks if the date range approximately matches a common preset (with 1-day tolerance for edge cases).
    /// </summary>
    public static bool IsApproximateRange(DateTime? from, DateTime? to, int daysBack, int tolerance = 1)
    {
        if (from == null && to == null) return false;
        
        var today = DateTime.Today;
        var start = from?.Date ?? today;
        var end = to?.Date ?? today;
        
        var expectedStart = today.AddDays(-daysBack);
        var daysDiff = Math.Abs((start - expectedStart).Days);
        
        return daysDiff <= tolerance && end == today;
    }
    
    /// <summary>
    /// Checks if the date range approximately matches a month-based preset (with 1-day tolerance).
    /// </summary>
    public static bool IsApproximateMonthRange(DateTime? from, DateTime? to, int monthsBack, int tolerance = 1)
    {
        if (from == null && to == null) return false;
        
        var today = DateTime.Today;
        var start = from?.Date ?? today;
        var end = to?.Date ?? today;
        
        var expectedStart = today.AddMonths(-monthsBack);
        var daysDiff = Math.Abs((start - expectedStart).Days);
        
        return daysDiff <= tolerance && end == today;
    }
}
