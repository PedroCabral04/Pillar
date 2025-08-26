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
}
