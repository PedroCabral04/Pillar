namespace erp.DTOs.Dashboard;

public class DashboardLayout
{
    public required string UserId { get; set; }
    public List<WidgetConfiguration> Widgets { get; set; } = new();
    public string LayoutType { get; set; } = "grid"; // grid, list, compact
    public int Columns { get; set; } = 3;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public class WidgetConfiguration
{
    public required string WidgetId { get; set; }
    public required string ProviderKey { get; set; }
    public required string WidgetKey { get; set; }
    public int Order { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int Width { get; set; } = 1; // Grid units
    public int Height { get; set; } = 1; // Grid units
    public bool IsVisible { get; set; } = true;
    public bool IsCollapsed { get; set; } = false;
    public Dictionary<string, object>? CustomSettings { get; set; }
}

public class WidgetCatalogItem
{
    public required string ProviderKey { get; set; }
    public required string WidgetKey { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool RequiresConfiguration { get; set; }
    public string[]? RequiredRoles { get; set; }
}

public class SaveLayoutRequest
{
    public required List<WidgetConfiguration> Widgets { get; set; }
    public string LayoutType { get; set; } = "grid";
    public int Columns { get; set; } = 3;
}

public class AddWidgetRequest
{
    public required string ProviderKey { get; set; }
    public required string WidgetKey { get; set; }
    public int? Row { get; set; }
    public int? Column { get; set; }
}

public class UpdateWidgetRequest
{
    public int? Order { get; set; }
    public int? Row { get; set; }
    public int? Column { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsCollapsed { get; set; }
    public Dictionary<string, object>? CustomSettings { get; set; }
}
