using erp.DTOs.Inventory;
using erp.Models.Inventory;

namespace erp.Services.Inventory;

public static class StockAdjustmentCalculator
{
    public static CreateStockMovementDto CreateAdjustmentMovement(
        int productId,
        decimal currentBackendStock,
        decimal targetStock,
        decimal unitCost,
        string notes)
    {
        var difference = targetStock - currentBackendStock;

        return new CreateStockMovementDto
        {
            ProductId = productId,
            Type = difference > 0 ? (int)MovementType.In : (int)MovementType.Out,
            Reason = (int)MovementReason.Adjustment,
            Quantity = Math.Abs(difference),
            UnitCost = unitCost,
            MovementDate = DateTime.UtcNow,
            Notes = notes
        };
    }
}
