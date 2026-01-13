using erp.DTOs.ServiceOrders;

namespace erp.Services.ServiceOrders;

/// <summary>
/// Interface para serviço de Ordens de Serviço
/// </summary>
public interface IServiceOrderService
{
    /// <summary>
    /// Cria uma nova ordem de serviço
    /// </summary>
    Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto dto, int userId, int tenantId);

    /// <summary>
    /// Busca uma ordem de serviço por ID
    /// </summary>
    Task<ServiceOrderDto?> GetByIdAsync(int id);

    /// <summary>
    /// Busca ordens de serviço com filtros e paginação
    /// </summary>
    Task<(List<ServiceOrderDto> items, int total)> SearchAsync(
        string? search,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        int? customerId,
        int page,
        int pageSize);

    /// <summary>
    /// Atualiza uma ordem de serviço existente
    /// </summary>
    Task<ServiceOrderDto> UpdateAsync(int id, UpdateServiceOrderDto dto);

    /// <summary>
    /// Deleta (cancela) uma ordem de serviço
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Atualiza apenas o status de uma ordem de serviço
    /// </summary>
    Task<ServiceOrderDto> UpdateStatusAsync(int id, string status, string? notes);

    /// <summary>
    /// Marca a ordem como concluída
    /// </summary>
    Task<ServiceOrderDto> CompleteAsync(int id);

    /// <summary>
    /// Marca a ordem como entregue
    /// </summary>
    Task<ServiceOrderDto> DeliverAsync(int id);

    /// <summary>
    /// Obtém o total de receitas de serviços em um período
    /// </summary>
    Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Obtém resumo de estatísticas por status
    /// </summary>
    Task<List<ServiceOrderStatusSummaryDto>> GetStatusSummaryAsync();

    /// <summary>
    /// Gera o próximo número de ordem de serviço
    /// </summary>
    Task<string> GenerateNextOrderNumberAsync(int tenantId);
}
