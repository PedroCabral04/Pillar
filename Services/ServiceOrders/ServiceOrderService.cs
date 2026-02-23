using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.ServiceOrders;
using erp.Models.Financial;
using erp.Models.ServiceOrders;
using erp.Mappings;
using erp.Services.Financial;

namespace erp.Services.ServiceOrders;

/// <summary>
/// Implementação do serviço de Ordens de Serviço
/// </summary>
public class ServiceOrderService : IServiceOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ServiceOrderMapper _mapper;
    private readonly ILogger<ServiceOrderService> _logger;

    public ServiceOrderService(
        ApplicationDbContext context,
        ServiceOrderMapper mapper,
        ILogger<ServiceOrderService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    private const int MaxOrderNumberRetries = 3;

    public async Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto dto, int userId, int tenantId)
    {
        for (int retryCount = 0; retryCount <= MaxOrderNumberRetries; retryCount++)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                // Generate order number
                var orderNumber = await GenerateNextOrderNumberAsync(tenantId);

                var order = _mapper.ToEntity(dto);
                order.OrderNumber = orderNumber;
                order.UserId = userId;
                order.TenantId = tenantId;
                order.CreatedAt = DateTime.UtcNow;

                // Calculate totals
                order.TotalAmount = dto.Items.Sum(i => i.Price);

                // Validate discount doesn't exceed total
                if (dto.DiscountAmount > order.TotalAmount)
                    throw new InvalidOperationException("Desconto não pode ser maior que o valor total");

                order.NetAmount = order.TotalAmount - dto.DiscountAmount;

                // Set items tenant ID
                foreach (var item in order.Items)
                {
                    item.TenantId = tenantId;
                }

                _context.ServiceOrders.Add(order);
                await _context.SaveChangesAsync();

                await EnsureFinancialReceivableForServiceOrderAsync(order);
                await transaction.CommitAsync();

                _logger.LogInformation("Ordem de Serviço {OrderId} criada com sucesso. Número: {OrderNumber}",
                    order.Id, order.OrderNumber);

                return await GetByIdWithIncludesAsync(order.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex) && retryCount < MaxOrderNumberRetries)
            {
                // Retry on order number collision (race condition)
                await transaction.RollbackAsync();
                _logger.LogWarning("Colisão no número da ordem de serviço, tentativa {RetryCount} de {MaxRetries}",
                    retryCount + 1, MaxOrderNumberRetries);
                continue;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar ordem de serviço");
                throw;
            }
        }

        throw new InvalidOperationException($"Falha ao criar ordem de serviço após {MaxOrderNumberRetries} tentativas devido a conflitos de concorrência.");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Check for PostgreSQL unique constraint violation (error code 23505)
        return ex.InnerException?.Message.Contains("23505") == true ||
               ex.InnerException?.Message.Contains("duplicate key") == true ||
               ex.InnerException?.Message.Contains("unique constraint") == true;
    }

    public async Task<ServiceOrderDto?> GetByIdAsync(int id)
    {
        var order = await _context.ServiceOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return null;

        var dto = _mapper.ToDto(order);
        dto.StatusDisplay = GetStatusDisplay(order.Status);
        dto.UserName = order.User?.UserName ?? "N/A";

        if (order.Customer != null)
        {
            dto.Customer = new()
            {
                Id = order.Customer.Id,
                Document = order.Customer.Document,
                Name = order.Customer.Name,
                Phone = order.Customer.Phone,
                Mobile = order.Customer.Mobile,
                Email = order.Customer.Email,
                Address = order.Customer.Address,
                Number = order.Customer.Number,
                Complement = order.Customer.Complement,
                Neighborhood = order.Customer.Neighborhood,
                City = order.Customer.City,
                State = order.Customer.State,
                ZipCode = order.Customer.ZipCode
            };
        }

        return dto;
    }

    public async Task<(List<ServiceOrderDto> items, int total)> SearchAsync(
        string? search,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        int? customerId,
        int page,
        int pageSize)
    {
        var query = _context.ServiceOrders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                (o.DeviceBrand != null && o.DeviceBrand.Contains(search)) ||
                (o.DeviceModel != null && o.DeviceModel.Contains(search)) ||
                (o.SerialNumber != null && o.SerialNumber.Contains(search)) ||
                (o.Customer != null && o.Customer.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.EntryDate <= endDate.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.EntryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        var dtos = orders.Select(order =>
        {
            var dto = _mapper.ToDto(order);
            dto.StatusDisplay = GetStatusDisplay(order.Status);
            dto.UserName = order.User?.UserName ?? "N/A";

            if (order.Customer != null)
            {
                dto.Customer = new()
                {
                    Id = order.Customer.Id,
                    Document = order.Customer.Document,
                    Name = order.Customer.Name,
                    Phone = order.Customer.Phone,
                    Mobile = order.Customer.Mobile,
                    Email = order.Customer.Email
                };
            }

            return dto;
        }).ToList();

        return (dtos, total);
    }

    public async Task<ServiceOrderDto> UpdateAsync(int id, UpdateServiceOrderDto dto)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Ordem de serviço {id} não encontrada");

        // Update entity
        _mapper.UpdateEntity(dto, order);
        order.UpdatedAt = DateTime.UtcNow;

        // Update items if provided
        if (dto.Items != null)
        {
            // Remove items not in the list (by Id match)
            var itemIdsToKeep = dto.Items
                .Where(i => i.Id.HasValue)
                .Select(i => i.Id!.Value)
                .ToHashSet();

            var itemsToRemove = order.Items
                .Where(existing => !itemIdsToKeep.Contains(existing.Id))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                _context.ServiceOrderItems.Remove(item);
            }

            // Update or add items
            foreach (var itemDto in dto.Items)
            {
                var existingItem = itemDto.Id.HasValue 
                    ? order.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value) 
                    : null;
                if (existingItem != null)
                {
                    _mapper.UpdateEntity(itemDto, existingItem);
                }
                else
                {
                    var newItem = _mapper.ToEntity(itemDto);
                    newItem.ServiceOrderId = order.Id;
                    newItem.TenantId = order.TenantId;
                    order.Items.Add(newItem); // Add to collection directly for proper total calculation
                }
            }
        }

        // Recalculate totals from the updated collection
        order.TotalAmount = order.Items.Sum(i => i.Price);

        // Validate discount doesn't exceed total
        if (dto.DiscountAmount > order.TotalAmount)
            throw new InvalidOperationException("Desconto não pode ser maior que o valor total");

        order.NetAmount = order.TotalAmount - dto.DiscountAmount;

        await EnsureFinancialReceivableForServiceOrderAsync(order);
        if (order.Status == ServiceOrderStatus.Cancelled.ToString())
        {
            await CancelFinancialReceivableForServiceOrderAsync(order);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ordem de Serviço {OrderId} atualizada com sucesso", id);

        return await GetByIdAsync(id) ?? throw new InvalidOperationException("Erro ao recuperar ordem atualizada");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null)
            return false;

        // Soft delete by setting status to Cancelled
        order.Status = ServiceOrderStatus.Cancelled.ToString();
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ordem de Serviço {OrderId} cancelada", id);
        return true;
    }

    public async Task<ServiceOrderDto> UpdateStatusAsync(int id, string status, string? notes)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null)
            throw new InvalidOperationException($"Ordem de serviço {id} não encontrada");

        // Validate status transition
        if (!IsValidStatusTransition(order.Status, status))
        {
            throw new InvalidOperationException($"Transição de status inválida: de {order.Status} para {status}");
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            order.TechnicalNotes = string.IsNullOrWhiteSpace(order.TechnicalNotes)
                ? notes
                : $"{order.TechnicalNotes}\n\n[Status alterado para {status} em {DateTime.Now:dd/MM/yyyy HH:mm}]\n{notes}";
        }

        // Set completion date if completing
        if (status == ServiceOrderStatus.Completed.ToString() && !order.ActualCompletionDate.HasValue)
        {
            order.ActualCompletionDate = DateTime.UtcNow;
        }

        await EnsureFinancialReceivableForServiceOrderAsync(order);

        if (status == ServiceOrderStatus.Cancelled.ToString())
        {
            await CancelFinancialReceivableForServiceOrderAsync(order);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ordem de Serviço {OrderId} status alterado para {Status}", id, status);

        return await GetByIdAsync(id) ?? throw new InvalidOperationException("Erro ao recuperar ordem atualizada");
    }

    public async Task<ServiceOrderDto> CompleteAsync(int id)
    {
        return await UpdateStatusAsync(id, ServiceOrderStatus.Completed.ToString(), "Serviço concluído");
    }

    public async Task<ServiceOrderDto> DeliverAsync(int id)
    {
        return await UpdateStatusAsync(id, ServiceOrderStatus.Delivered.ToString(), "Entregue ao cliente");
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.ServiceOrders
            .Where(o => o.EntryDate >= startDate && o.EntryDate <= endDate)
            .Where(o => o.Status != ServiceOrderStatus.Cancelled.ToString())
            .SumAsync(o => o.NetAmount);
    }

    public async Task<List<ServiceOrderStatusSummaryDto>> GetStatusSummaryAsync()
    {
        // First materialize the query, then map StatusDisplay in memory
        var summaries = await _context.ServiceOrders
            .GroupBy(o => o.Status)
            .Select(g => new 
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.NetAmount)
            })
            .ToListAsync();

        return summaries.Select(s => new ServiceOrderStatusSummaryDto
        {
            Status = s.Status,
            StatusDisplay = GetStatusDisplay(s.Status),
            Count = s.Count,
            TotalAmount = s.TotalAmount
        }).ToList();
    }

    public async Task<string> GenerateNextOrderNumberAsync(int tenantId)
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"OS{today}";

        // Find the last order number for today
        var lastOrderNumber = await _context.ServiceOrders
            .Where(o => o.TenantId == tenantId && o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastOrderNumber != null)
        {
            var lastSequence = lastOrderNumber.Substring(prefix.Length);
            if (int.TryParse(lastSequence, out int seq))
            {
                sequence = seq + 1;
            }
        }

        return $"{prefix}{sequence:D3}";
    }

    private async Task<ServiceOrderDto> GetByIdWithIncludesAsync(int id)
    {
        var order = await _context.ServiceOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Ordem de serviço {id} não encontrada");

        var dto = _mapper.ToDto(order);
        dto.StatusDisplay = GetStatusDisplay(order.Status);
        dto.UserName = order.User?.UserName ?? "N/A";

        if (order.Customer != null)
        {
            dto.Customer = new()
            {
                Id = order.Customer.Id,
                Document = order.Customer.Document,
                Name = order.Customer.Name,
                Phone = order.Customer.Phone,
                Mobile = order.Customer.Mobile,
                Email = order.Customer.Email,
                Address = order.Customer.Address,
                Number = order.Customer.Number,
                Complement = order.Customer.Complement,
                Neighborhood = order.Customer.Neighborhood,
                City = order.Customer.City,
                State = order.Customer.State,
                ZipCode = order.Customer.ZipCode
            };
        }

        return dto;
    }

    private static string GetStatusDisplay(string status)
    {
        return status switch
        {
            "Open" => "Aberto",
            "InProgress" => "Em Andamento",
            "WaitingCustomer" => "Aguardando Cliente",
            "WaitingParts" => "Aguardando Peças",
            "Completed" => "Concluído",
            "Delivered" => "Entregue",
            "Cancelled" => "Cancelado",
            _ => status
        };
    }

    private static bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        var validTransitions = new Dictionary<string, HashSet<string>>
        {
            ["Open"] = new() { "InProgress", "Cancelled" },
            ["InProgress"] = new() { "WaitingCustomer", "WaitingParts", "Completed", "Cancelled" },
            ["WaitingCustomer"] = new() { "InProgress", "Cancelled" },
            ["WaitingParts"] = new() { "InProgress", "Cancelled" },
            ["Completed"] = new() { "Delivered" },
            ["Delivered"] = new(), // Final state
            ["Cancelled"] = new()  // Final state
        };

        return validTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);
    }

    private async Task EnsureFinancialReceivableForServiceOrderAsync(ServiceOrder order)
    {
        var isConcluded = order.Status == ServiceOrderStatus.Completed.ToString() || order.Status == ServiceOrderStatus.Delivered.ToString();
        if (!isConcluded || !order.CustomerId.HasValue)
        {
            return;
        }

        var existing = await _context.AccountsReceivable
            .FirstOrDefaultAsync(a => a.InvoiceNumber == order.OrderNumber && a.CustomerId == order.CustomerId.Value);

        if (existing != null)
        {
            return;
        }

        var method = PaymentMethodResolver.FromSaleText(order.PaymentMethod);
        var isPaid = PaymentMethodResolver.IsImmediatelyPaid(method);
        var now = DateTime.UtcNow;
        var competenceDate = order.ActualCompletionDate ?? order.EntryDate;

        var receivable = new AccountReceivable
        {
            TenantId = order.TenantId,
            CustomerId = order.CustomerId.Value,
            InvoiceNumber = order.OrderNumber,
            OriginalAmount = order.NetAmount,
            IssueDate = competenceDate,
            DueDate = competenceDate,
            PaidAmount = isPaid ? order.NetAmount : 0,
            PaymentDate = isPaid ? now : null,
            Status = isPaid ? AccountStatus.Paid : AccountStatus.Pending,
            PaymentMethod = method,
            Notes = $"Gerado automaticamente pela ordem de servico {order.OrderNumber}",
            CreatedAt = now,
            CreatedByUserId = order.UserId,
            ReceivedByUserId = isPaid ? order.UserId : null
        };

        _context.AccountsReceivable.Add(receivable);
    }

    private async Task CancelFinancialReceivableForServiceOrderAsync(ServiceOrder order)
    {
        var receivable = await _context.AccountsReceivable
            .FirstOrDefaultAsync(a => a.InvoiceNumber == order.OrderNumber && a.CustomerId == order.CustomerId);

        if (receivable == null)
        {
            return;
        }

        receivable.Status = AccountStatus.Cancelled;
        receivable.UpdatedAt = DateTime.UtcNow;
    }
}
