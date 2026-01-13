using Riok.Mapperly.Abstractions;
using erp.Models.ServiceOrders;
using erp.DTOs.ServiceOrders;

namespace erp.Mappings;

/// <summary>
/// Mapper para Ordens de Serviço usando Mapperly
/// </summary>
[Mapper]
public partial class ServiceOrderMapper
{
    // ServiceOrder mappings
    public partial ServiceOrderDto ToDto(ServiceOrder order);

    [MapProperty(nameof(ServiceOrder.User.UserName), nameof(ServiceOrderDto.UserName))]
    public partial ServiceOrderDto ToDtoWithRelations(ServiceOrder order);

    public partial ServiceOrder ToEntity(CreateServiceOrderDto dto);
    public partial void UpdateEntity(UpdateServiceOrderDto dto, ServiceOrder entity);

    // Helper mappings
    private partial CustomerMiniDto? MapCustomer(Models.Sales.Customer? customer);

    // ServiceOrderItem mappings
    [UserMapping(Default = true)]
    public partial ServiceOrderItemDto ToDto(ServiceOrderItem item);

    public partial ServiceOrderItem ToEntity(CreateServiceOrderItemDto dto);
    public partial void UpdateEntity(UpdateServiceOrderItemDto dto, ServiceOrderItem entity);
    public partial ServiceOrderItem ToEntity(UpdateServiceOrderItemDto dto);
}
