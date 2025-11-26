using erp.Data;
using erp.DTOs.Dashboard;
using erp.Models.Dashboard;
using erp.Services.Dashboard;
using Microsoft.EntityFrameworkCore;
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
    Task SetWidgetRolesAsync(string providerKey, string widgetKey, string[]? roles, int? modifiedByUserId = null);
}

public class DashboardLayoutService : IDashboardLayoutService
{
    private readonly ApplicationDbContext _context;
    private readonly IDashboardRegistry _registry;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DashboardLayoutService(ApplicationDbContext context, IDashboardRegistry registry)
    {
        _context = context;
        _registry = registry;
    }

    public async Task<DashboardLayout> GetUserLayoutAsync(string userId)
    {
        if (!int.TryParse(userId, out var userIdInt))
        {
            return CreateDefaultLayout(userId);
        }

        var dbLayout = await _context.UserDashboardLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userIdInt);

        if (dbLayout == null)
        {
            // Create default layout and save to database
            var defaultLayout = CreateDefaultLayout(userId);
            await SaveUserLayoutAsync(userId, defaultLayout);
            return defaultLayout;
        }

        var widgets = DeserializeWidgets(dbLayout.LayoutJson);
        
        return new DashboardLayout
        {
            UserId = userId,
            LayoutType = dbLayout.LayoutType,
            Columns = dbLayout.Columns,
            Widgets = widgets,
            LastModified = dbLayout.LastModified
        };
    }

    public async Task SaveUserLayoutAsync(string userId, DashboardLayout layout)
    {
        if (!int.TryParse(userId, out var userIdInt))
        {
            return;
        }

        var dbLayout = await _context.UserDashboardLayouts
            .FirstOrDefaultAsync(x => x.UserId == userIdInt);

        var widgetsJson = JsonSerializer.Serialize(layout.Widgets, _jsonOptions);

        if (dbLayout == null)
        {
            dbLayout = new UserDashboardLayout
            {
                UserId = userIdInt,
                LayoutJson = widgetsJson,
                LayoutType = layout.LayoutType,
                Columns = layout.Columns,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.UserDashboardLayouts.Add(dbLayout);
        }
        else
        {
            dbLayout.LayoutJson = widgetsJson;
            dbLayout.LayoutType = layout.LayoutType;
            dbLayout.Columns = layout.Columns;
            dbLayout.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
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
        if (!int.TryParse(userId, out var userIdInt))
        {
            return;
        }

        // Delete existing layout
        var existingLayout = await _context.UserDashboardLayouts
            .FirstOrDefaultAsync(x => x.UserId == userIdInt);
        
        if (existingLayout != null)
        {
            _context.UserDashboardLayouts.Remove(existingLayout);
            await _context.SaveChangesAsync();
        }

        // Create and save default layout
        var defaultLayout = CreateDefaultLayout(userId);
        await SaveUserLayoutAsync(userId, defaultLayout);
    }

    public List<WidgetCatalogItem> GetAvailableWidgets(string[] userRoles)
    {
        var catalog = new List<WidgetCatalogItem>();
        var definitions = _registry.ListAll();

        // Load all role overrides from database
        var overrides = _context.WidgetRoleConfigurations
            .AsNoTracking()
            .ToDictionary(
                x => $"{x.ProviderKey}__{x.WidgetKey}",
                x => x.RolesJson,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var def in definitions)
        {
            var key = $"{def.ProviderKey}__{def.WidgetKey}";
            string[]? effectiveRoles = def.RequiredRoles;

            // Apply database override if present
            if (overrides.TryGetValue(key, out var rolesJson) && !string.IsNullOrEmpty(rolesJson))
            {
                effectiveRoles = JsonSerializer.Deserialize<string[]>(rolesJson, _jsonOptions);
            }

            catalog.Add(new WidgetCatalogItem
            {
                ProviderKey = def.ProviderKey,
                WidgetKey = def.WidgetKey,
                Title = def.Title,
                Description = def.Description ?? "Dashboard widget",
                Icon = def.Icon,
                Category = def.ProviderKey,
                RequiresConfiguration = false,
                RequiredRoles = effectiveRoles
            });
        }

        return catalog;
    }

    public async Task<string[]?> GetWidgetRolesAsync(string providerKey, string widgetKey)
    {
        var config = await _context.WidgetRoleConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProviderKey == providerKey && x.WidgetKey == widgetKey);

        if (config != null && !string.IsNullOrEmpty(config.RolesJson))
        {
            return JsonSerializer.Deserialize<string[]>(config.RolesJson, _jsonOptions);
        }

        // Fall back to provider's default
        var def = _registry.Find(providerKey, widgetKey);
        return def?.RequiredRoles;
    }

    public async Task SetWidgetRolesAsync(string providerKey, string widgetKey, string[]? roles, int? modifiedByUserId = null)
    {
        var config = await _context.WidgetRoleConfigurations
            .FirstOrDefaultAsync(x => x.ProviderKey == providerKey && x.WidgetKey == widgetKey);

        var rolesJson = roles != null && roles.Length > 0
            ? JsonSerializer.Serialize(roles, _jsonOptions)
            : null;

        if (config == null)
        {
            config = new WidgetRoleConfiguration
            {
                ProviderKey = providerKey,
                WidgetKey = widgetKey,
                RolesJson = rolesJson,
                LastModified = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId
            };
            _context.WidgetRoleConfigurations.Add(config);
        }
        else
        {
            config.RolesJson = rolesJson;
            config.LastModified = DateTime.UtcNow;
            config.ModifiedByUserId = modifiedByUserId;
        }

        await _context.SaveChangesAsync();
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

        // Add default widgets - take first 6 from all available providers
        var definitions = _registry.ListAll().Take(6);
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

    private List<WidgetConfiguration> DeserializeWidgets(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<WidgetConfiguration>>(json, _jsonOptions) ?? new List<WidgetConfiguration>();
        }
        catch
        {
            return new List<WidgetConfiguration>();
        }
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
