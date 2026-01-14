namespace erp.DTOs;

/// <summary>
/// Generic paginated result wrapper for API responses
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Summary totals grouped by account status
/// </summary>
public class TotalByStatusResult
{
    public decimal Pending { get; set; }
    public decimal Overdue { get; set; }
    public decimal Paid { get; set; }
    public decimal PartiallyPaid { get; set; }
    public decimal Cancelled { get; set; }
}
