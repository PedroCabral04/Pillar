namespace erp.DTOs.Shared;

/// <summary>
/// Standard error response for API endpoints
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Detailed error information (optional)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Application-specific error code (optional)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
