namespace erp.DTOs.Audit;

public class AuditLogDto
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? EntityDescription { get; set; }
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }
    public string? References { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalInfo { get; set; }
    
    /// <summary>
    /// Lista de propriedades alteradas deserializada para exibição
    /// </summary>
    public List<PropertyChangeDto>? ParsedChangedProperties { get; set; }
    
    /// <summary>
    /// Dicionário de referências deserializado para exibição
    /// </summary>
    public Dictionary<string, string?>? ParsedReferences { get; set; }
}

/// <summary>
/// DTO para representar uma propriedade alterada
/// </summary>
public class PropertyChangeDto
{
    public string PropertyName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}

public class AuditLogFilterDto
{
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TenantId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditLogPagedResultDto
{
    public List<AuditLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
