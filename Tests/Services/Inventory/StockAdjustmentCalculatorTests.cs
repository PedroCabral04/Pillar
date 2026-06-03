using erp.Models.Inventory;
using erp.Services.Inventory;

namespace erp.Tests.Services.Inventory;

public class StockAdjustmentCalculatorTests
{
    [Fact]
    public void CreateAdjustmentMovement_UsesCurrentBackendStockAsBaseline()
    {
        var movement = StockAdjustmentCalculator.CreateAdjustmentMovement(
            productId: 42,
            currentBackendStock: 8m,
            targetStock: 12m,
            unitCost: 5m,
            notes: "adjustment");

        movement.Type.Should().Be((int)MovementType.In);
        movement.Quantity.Should().Be(4m);
    }

    [Fact]
    public void CreateAdjustmentMovement_WhenTargetIsBelowCurrentStock_CreatesOutboundMovement()
    {
        var movement = StockAdjustmentCalculator.CreateAdjustmentMovement(
            productId: 42,
            currentBackendStock: 12m,
            targetStock: 8m,
            unitCost: 5m,
            notes: "adjustment");

        movement.Type.Should().Be((int)MovementType.Out);
        movement.Quantity.Should().Be(4m);
    }
}
