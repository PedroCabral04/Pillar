namespace erp.DTOs.Inventory;

public class ImportProductsFormDto
{
    public IFormFile? File { get; set; }
}

public class ProductImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<ProductImportIssueDto> Issues { get; set; } = new();
}

public class ProductImportIssueDto
{
    public int RowNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public bool IsSkipped { get; set; }
}
