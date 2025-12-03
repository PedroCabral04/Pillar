using erp.DTOs.Preferences;

namespace erp.Services;

/// <summary>
/// Service to provide table-related preferences to components
/// </summary>
public interface ITablePreferenceService
{
    /// <summary>
    /// Gets the preferred page size for tables
    /// </summary>
    int GetPageSize();

    /// <summary>
    /// Gets the default sort for a specific module (e.g., "Clientes", "Produtos")
    /// </summary>
    /// <returns>Tuple of (sortField, isDescending) or null if not configured</returns>
    (string field, bool descending)? GetDefaultSort(string moduleName);

    /// <summary>
    /// Gets the visible columns for a specific module
    /// </summary>
    /// <returns>Array of column names or null if not configured</returns>
    string[]? GetVisibleColumns(string moduleName);

    /// <summary>
    /// Checks if a column should be visible for a module
    /// </summary>
    bool IsColumnVisible(string moduleName, string columnName);
}

public class TablePreferenceService : ITablePreferenceService
{
    private readonly PreferenceService _preferenceService;

    public TablePreferenceService(PreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    public int GetPageSize()
    {
        return _preferenceService.CurrentPreferences.Tables.PageSize;
    }

    public (string field, bool descending)? GetDefaultSort(string moduleName)
    {
        var sortDict = _preferenceService.CurrentPreferences.Tables.DefaultSortPerModule;
        if (sortDict == null || !sortDict.TryGetValue(moduleName, out var sortValue))
            return null;

        // Parse format "FieldName:asc" or "FieldName:desc"
        var parts = sortValue.Split(':');
        if (parts.Length < 1)
            return null;

        var field = parts[0].Trim();
        var descending = parts.Length > 1 && parts[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (field, descending);
    }

    public string[]? GetVisibleColumns(string moduleName)
    {
        var columnsDict = _preferenceService.CurrentPreferences.Tables.VisibleColumnsPerModule;
        if (columnsDict == null || !columnsDict.TryGetValue(moduleName, out var columns))
            return null;

        return columns;
    }

    public bool IsColumnVisible(string moduleName, string columnName)
    {
        var columns = GetVisibleColumns(moduleName);
        
        // If no configuration, all columns are visible
        if (columns == null || columns.Length == 0)
            return true;

        return columns.Contains(columnName, StringComparer.OrdinalIgnoreCase);
    }
}
