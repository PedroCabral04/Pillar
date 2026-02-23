namespace erp.Shared;

/// <summary>
/// Standard paginated response for API endpoints
/// </summary>
/// <typeparam name="T">Type of items in the response</typeparam>
public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Helper class for creating consistent paginated responses
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Creates a standardized paginated response
    /// </summary>
    /// <typeparam name="T">Type of items in the response</typeparam>
    /// <param name="items">The items for the current page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    /// <param name="page">Current page number (1-indexed)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A paginated response with calculated metadata</returns>
    public static PaginatedResponse<T> CreateResponse<T>(
        IEnumerable<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<T>
        {
            Items = items,
            TotalItems = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    /// <summary>
    /// Creates a standardized paginated response from an anonymous object
    /// </summary>
    /// <param name="items">The items for the current page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    /// <param name="page">Current page number (1-indexed)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>An anonymous object with pagination metadata</returns>
    public static object CreateResponse(
        object items,
        int totalCount,
        int page,
        int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new
        {
            items,
            totalItems = totalCount,
            page,
            pageSize,
            totalPages,
            hasNextPage = page < totalPages,
            hasPreviousPage = page > 1
        };
    }
}
