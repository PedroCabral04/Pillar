using erp.DTOs.Inventory;

namespace erp.Services.Inventory;

public static class QuickProductDefaults
{
    public static void Apply(CreateProductDto dto, decimal? initialStock)
    {
        if (string.IsNullOrWhiteSpace(dto.Sku))
        {
            dto.Sku = GenerateSku();
        }

        dto.CostPrice = dto.SalePrice;
        dto.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "UN" : dto.Unit;
        dto.UnitsPerBox = dto.UnitsPerBox <= 0 ? 1 : dto.UnitsPerBox;
        dto.IsActive = true;
        dto.Status = 0;
        dto.CurrentStock = initialStock.GetValueOrDefault() > 0 ? initialStock!.Value : 0;
    }

    private static string GenerateSku()
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"PRD-{stamp}-{suffix}";
    }
}
