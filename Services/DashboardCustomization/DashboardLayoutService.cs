using erp.DTOs.Dashboard;
using erp.Services.Dashboard;
using System.Text.Json;

namespace erp.Services.DashboardCustomization;

public interface IDashboardLayoutService
{
    Task<DashboardLayout> GetUserLayoutAsync(string userId);
    Task SaveUserLayoutAsync(string userId, DashboardLayout layout);
    Task<WidgetConfiguration?> AddWidgetAsync(string userId, string providerKey, string widgetKey, int? row = null, int? column = null);
    Task<bool> RemoveWidgetAsync(string userId, string widgetId);
    Task<bool> UpdateWidgetAsync(string userId, string widgetId, UpdateWidgetRequest request);
    Task<bool> ReorderWidgetsAsync(string userId, List<string> widgetOrder);
    Task ResetToDefaultAsync(string userId);
    List<WidgetCatalogItem> GetAvailableWidgets(string[] userRoles);
}

public class DashboardLayoutService : IDashboardLayoutService
{
    private readonly IDashboardRegistry _registry;
    private readonly Dictionary<string, DashboardLayout> _layoutCache = new();

    public DashboardLayoutService(IDashboardRegistry registry)
    {
        _registry = registry;
    }

    public Task<DashboardLayout> GetUserLayoutAsync(string userId)
    {
        if (_layoutCache.TryGetValue(userId, out var layout))
        {
            return Task.FromResult(layout);
        }

        // Create default layout
        layout = CreateDefaultLayout(userId);
        _layoutCache[userId] = layout;
        return Task.FromResult(layout);
    }

    public Task SaveUserLayoutAsync(string userId, DashboardLayout layout)
    {
        layout.UserId = userId;
        layout.LastModified = DateTime.UtcNow;
        _layoutCache[userId] = layout;
        return Task.CompletedTask;
    }

    public async Task<WidgetConfiguration?> AddWidgetAsync(string userId, string providerKey, string widgetKey, int? row = null, int? column = null)
    {
        var layout = await GetUserLayoutAsync(userId);
        
        // Check if widget already exists
        if (layout.Widgets.Any(w => w.ProviderKey == providerKey && w.WidgetKey == widgetKey))
        {
            return null;
        }

        var widgetId = $"{providerKey}_{widgetKey}_{Guid.NewGuid():N}";
        var maxOrder = layout.Widgets.Any() ? layout.Widgets.Max(w => w.Order) : -1;

        var widgetConfig = new WidgetConfiguration
        {
            WidgetId = widgetId,
            ProviderKey = providerKey,
            WidgetKey = widgetKey,
            Order = maxOrder + 1,
            Row = row ?? CalculateNextRow(layout),
            Column = column ?? CalculateNextColumn(layout),
            Width = 1,
            Height = 1,
            IsVisible = true,
            IsCollapsed = false
        };

        layout.Widgets.Add(widgetConfig);
        await SaveUserLayoutAsync(userId, layout);
        
        return widgetConfig;
    }

    public async Task<bool> RemoveWidgetAsync(string userId, string widgetId)
    {
        var layout = await GetUserLayoutAsync(userId);
        var widget = layout.Widgets.FirstOrDefault(w => w.WidgetId == widgetId);
        
        if (widget == null)
        {
            return false;
        }

        layout.Widgets.Remove(widget);
        await SaveUserLayoutAsync(userId, layout);
        
        return true;
    }

    public async Task<bool> UpdateWidgetAsync(string userId, string widgetId, UpdateWidgetRequest request)
    {
        var layout = await GetUserLayoutAsync(userId);
        var widget = layout.Widgets.FirstOrDefault(w => w.WidgetId == widgetId);
        
        if (widget == null)
        {
            return false;
        }

        if (request.Order.HasValue) widget.Order = request.Order.Value;
        if (request.Row.HasValue) widget.Row = request.Row.Value;
        if (request.Column.HasValue) widget.Column = request.Column.Value;
        if (request.Width.HasValue) widget.Width = request.Width.Value;
        if (request.Height.HasValue) widget.Height = request.Height.Value;
        if (request.IsVisible.HasValue) widget.IsVisible = request.IsVisible.Value;
        if (request.IsCollapsed.HasValue) widget.IsCollapsed = request.IsCollapsed.Value;
        if (request.CustomSettings != null) widget.CustomSettings = request.CustomSettings;

        await SaveUserLayoutAsync(userId, layout);
        
        return true;
    }

    public async Task<bool> ReorderWidgetsAsync(string userId, List<string> widgetOrder)
    {
        var layout = await GetUserLayoutAsync(userId);
        
        for (int i = 0; i < widgetOrder.Count; i++)
        {
            var widget = layout.Widgets.FirstOrDefault(w => w.WidgetId == widgetOrder[i]);
            if (widget != null)
            {
                widget.Order = i;
            }
        }

        await SaveUserLayoutAsync(userId, layout);
        return true;
    }

    public async Task ResetToDefaultAsync(string userId)
    {
        var defaultLayout = CreateDefaultLayout(userId);
        await SaveUserLayoutAsync(userId, defaultLayout);
    }

    public List<WidgetCatalogItem> GetAvailableWidgets(string[] userRoles)
    {
        var catalog = new List<WidgetCatalogItem>();
        var definitions = _registry.ListAll();

        foreach (var def in definitions)
        {
            catalog.Add(new WidgetCatalogItem
            {
                ProviderKey = def.ProviderKey,
                WidgetKey = def.WidgetKey,
                Title = def.Title,
                Description = def.Description ?? "Dashboard widget",
                Icon = def.Icon,
                Category = def.ProviderKey, // Use provider key as category
                RequiresConfiguration = false,
                RequiredRoles = null // TODO: Implement role-based widgets
            });
        }

        return catalog;
    }

    private DashboardLayout CreateDefaultLayout(string userId)
    {
        var layout = new DashboardLayout
        {
            UserId = userId,
            LayoutType = "grid",
            Columns = 3,
            Widgets = new List<WidgetConfiguration>()
        };

        // Add some default widgets
        var definitions = _registry.ListAll().Take(6); // Limit to first 6 widgets
        int order = 0;
        int row = 0;
        int col = 0;

        foreach (var def in definitions)
        {
            layout.Widgets.Add(new WidgetConfiguration
            {
                WidgetId = $"{def.ProviderKey}_{def.WidgetKey}_default",
                ProviderKey = def.ProviderKey,
                WidgetKey = def.WidgetKey,
                Order = order++,
                Row = row,
                Column = col,
                Width = 1,
                Height = 1,
                IsVisible = true,
                IsCollapsed = false
            });

            col++;
            if (col >= layout.Columns)
            {
                col = 0;
                row++;
            }
        }

        return layout;
    }

    private int CalculateNextRow(DashboardLayout layout)
    {
        if (!layout.Widgets.Any()) return 0;
        return layout.Widgets.Max(w => w.Row) + 1;
    }

    private int CalculateNextColumn(DashboardLayout layout)
    {
        if (!layout.Widgets.Any()) return 0;
        
        var lastRow = layout.Widgets.Max(w => w.Row);
        var widgetsInLastRow = layout.Widgets.Where(w => w.Row == lastRow).ToList();
        
        if (!widgetsInLastRow.Any()) return 0;
        
        var maxCol = widgetsInLastRow.Max(w => w.Column);
        return (maxCol + 1) % layout.Columns;
    }
}
