using Riok.Mapperly.Abstractions;
using erp.Models.Inventory;
using erp.DTOs.Inventory;
using System.Diagnostics.CodeAnalysis;

namespace erp.Mappings;

[Mapper]
[SuppressMessage("Mapper", "RMG020")]
public partial class StockMovementMapper
{
    // StockMovement -> StockMovementDto
    [UserMapping(Default = true)]
    public partial StockMovementDto MovementToMovementDto(StockMovement movement);
    public partial IEnumerable<StockMovementDto> MovementsToMovementDtos(IEnumerable<StockMovement> movements);
    
    // CreateStockMovementDto -> StockMovement
    [MapperIgnoreTarget(nameof(StockMovement.Id))]
    [MapperIgnoreTarget(nameof(StockMovement.Product))]
    [MapperIgnoreTarget(nameof(StockMovement.Warehouse))]
    [MapperIgnoreTarget(nameof(StockMovement.PreviousStock))]
    [MapperIgnoreTarget(nameof(StockMovement.CurrentStock))]
    [MapperIgnoreTarget(nameof(StockMovement.TotalCost))]
    [MapperIgnoreTarget(nameof(StockMovement.CreatedByUser))]
    [MapperIgnoreTarget(nameof(StockMovement.CreatedByUserId))]
    public partial StockMovement CreateMovementDtoToMovement(CreateStockMovementDto dto);
    
    // Mapeamento customizado para incluir nomes
    public StockMovementDto MapWithDetails(StockMovement movement)
    {
        var dto = MovementToMovementDto(movement);
        dto.ProductName = movement.Product?.Name ?? "";
        dto.ProductSku = movement.Product?.Sku ?? "";
        dto.WarehouseName = movement.Warehouse?.Name;
        dto.TypeName = TranslateMovementType(movement.Type);
        dto.ReasonName = TranslateMovementReason(movement.Reason);
        dto.CreatedByUserName = movement.CreatedByUser?.UserName ?? "";
        return dto;
    }

    private string TranslateMovementType(MovementType type)
    {
        return type switch
        {
            MovementType.In => "Entrada",
            MovementType.Out => "Saída",
            MovementType.Transfer => "Transferência",
            _ => type.ToString()
        };
    }

    private string TranslateMovementReason(MovementReason reason)
    {
        return reason switch
        {
            MovementReason.Purchase => "Compra",
            MovementReason.Sale => "Venda",
            MovementReason.Return => "Devolução",
            MovementReason.Adjustment => "Ajuste",
            MovementReason.Production => "Produção",
            MovementReason.Loss => "Perda",
            MovementReason.Donation => "Doação",
            MovementReason.Transfer => "Transferência",
            MovementReason.InitialBalance => "Saldo Inicial",
            _ => reason.ToString()
        };
    }
}
