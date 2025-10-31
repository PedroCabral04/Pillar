using System.Text.Json.Serialization;

namespace erp.DTOs.Sales;

public class PaginatedSalesResponse
{
    [JsonPropertyName("items")]
    public List<SaleDto> Items { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}
