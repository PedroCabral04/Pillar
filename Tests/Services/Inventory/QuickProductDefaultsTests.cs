using erp.DTOs.Inventory;
using erp.Services.Inventory;
using FluentAssertions;
using Xunit;

namespace erp.Tests.Services.Inventory;

public class QuickProductDefaultsTests
{
    [Fact]
    public void ApplyQuickDefaults_SetsExpectedDefaultValues()
    {
        var dto = new CreateProductDto
        {
            Name = "Mouse sem fio",
            CategoryId = 3,
            SalePrice = 120m,
        };

        QuickProductDefaults.Apply(dto, initialStock: 5m);

        dto.Sku.Should().StartWith("PRD-");
        dto.CostPrice.Should().Be(120m);
        dto.Unit.Should().Be("UN");
        dto.UnitsPerBox.Should().Be(1m);
        dto.IsActive.Should().BeTrue();
        dto.Status.Should().Be(0);
        dto.CurrentStock.Should().Be(5m);
    }

    [Fact]
    public void ApplyQuickDefaults_UsesZeroStockWhenInitialStockMissing()
    {
        var dto = new CreateProductDto
        {
            Name = "Teclado",
            CategoryId = 4,
            SalePrice = 200m,
        };

        QuickProductDefaults.Apply(dto, initialStock: null);

        dto.CurrentStock.Should().Be(0m);
    }
}
