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
    Task<string[]?> GetWidgetRolesAsync(string providerKey, string widgetKey);
    Task SetWidgetRolesAsync(string providerKey, string widgetKey, string[]? roles);
}

public class DashboardLayoutService : IDashboardLayoutService
{
    private readonly IDashboardRegistry _registry;
    private readonly Dictionary<string, DashboardLayout> _layoutCache = new();
    private readonly Dictionary<string, string[]?> _widgetRoleOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _overridesFilePath;
    private readonly object _fileLock = new();

    public DashboardLayoutService(IDashboardRegistry registry)
    {
        _registry = registry;
        // Persist role overrides to a simple JSON file next to the app base directory
        var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        _overridesFilePath = Path.Combine(baseDir, "widgetRoleOverrides.json");
        LoadOverridesFromFile();
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
            // Apply overrides if present
            var key = GetOverrideKey(def.ProviderKey, def.WidgetKey);
            var overridden = _widgetRoleOverrides.TryGetValue(key, out var oroles) ? oroles : def.RequiredRoles;

            catalog.Add(new WidgetCatalogItem
            {
                ProviderKey = def.ProviderKey,
                WidgetKey = def.WidgetKey,
                Title = def.Title,
                Description = def.Description ?? "Dashboard widget",
                Icon = def.Icon,
                Category = def.ProviderKey, // Use provider key as category
                RequiresConfiguration = false,
                RequiredRoles = overridden
            });
        }

        return catalog;
    }

    public Task<string[]?> GetWidgetRolesAsync(string providerKey, string widgetKey)
    {
        var key = GetOverrideKey(providerKey, widgetKey);
        if (_widgetRoleOverrides.TryGetValue(key, out var roles))
            return Task.FromResult(roles);

        var def = _registry.Find(providerKey, widgetKey);
        return Task.FromResult(def?.RequiredRoles);
    }

    public Task SetWidgetRolesAsync(string providerKey, string widgetKey, string[]? roles)
    {
        var key = GetOverrideKey(providerKey, widgetKey);
        if (roles == null || roles.Length == 0)
        {
            // Remove override to fall back to provider default
            if (_widgetRoleOverrides.ContainsKey(key))
            {
                _widgetRoleOverrides.Remove(key);
                SaveOverridesToFile();
            }
            return Task.CompletedTask;
        }

        _widgetRoleOverrides[key] = roles;
        SaveOverridesToFile();
        return Task.CompletedTask;
    }

    private string GetOverrideKey(string provider, string widget) => $"{provider}__{widget}";

    private void LoadOverridesFromFile()
    {
        try
        {
            if (!File.Exists(_overridesFilePath)) return;
            lock (_fileLock)
            {
                var json = File.ReadAllText(_overridesFilePath);
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]?>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _widgetRoleOverrides[kv.Key] = kv.Value;
                }
            }
        }
        catch
        {
            // ignore file errors; fall back to defaults
        }
    }

    private void SaveOverridesToFile()
    {
        try
        {
            lock (_fileLock)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_widgetRoleOverrides, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_overridesFilePath, json);
            }
        }
        catch
        {
            // ignore persistence errors for now
        }
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
