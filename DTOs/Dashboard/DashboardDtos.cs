using System.Text.Json.Serialization;

namespace erp.DTOs.Dashboard;

public enum DashboardChartType
{
    Line,
    Bar,
    Area,
    Donut,
    Pie
}

public record DashboardWidgetDefinition
{
    public required string ProviderKey { get; init; }
    public required string WidgetKey { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DashboardChartType ChartType { get; init; }
    public string? Icon { get; init; }
    public string? Unit { get; init; }
    // Optional roles required to view this widget. If null or empty, widget is available to all authenticated users.
    public string[]? RequiredRoles { get; init; }
}

public record DashboardQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? TopN { get; init; }
    public Dictionary<string, string>? Filters { get; init; }
}

public record ChartSeriesDto
{
    public required string Name { get; init; }
    public required List<decimal> Data { get; init; } = new();
}

public record ChartDataResponse
{
    public List<string>? Categories { get; init; }
    public required List<ChartSeriesDto> Series { get; init; } = new();
    public string? Subtitle { get; init; }
    public Dictionary<string, object>? Meta { get; init; }
    
    /// <summary>
    /// Dynamic title that overrides the widget definition title based on query context.
    /// If null, the original widget title from DashboardWidgetDefinition is used.
    /// </summary>
    public string? DynamicTitle { get; init; }
    
    /// <summary>
    /// Dynamic description that overrides the widget definition description based on query context.
    /// If null, the original widget description from DashboardWidgetDefinition is used.
    /// </summary>
    public string? DynamicDescription { get; init; }
    
    /// <summary>
    /// Indicates if this widget represents a current state snapshot (not affected by date filters).
    /// When true, UI should show an indicator that the widget shows "current state".
    /// </summary>
    public bool IsCurrentStateWidget { get; init; } = false;
    
    /// <summary>
    /// Human-readable period label (e.g., "01/12/2024 - 31/12/2024") for context display.
    /// </summary>
    public string? PeriodLabel { get; init; }
}
