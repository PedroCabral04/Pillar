using System.Text.Json.Serialization;

namespace erp.DTOs.Sales;

public class PaginatedCustomersResponse
{
    [JsonPropertyName("items")]
    public List<CustomerDto> Items { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}
