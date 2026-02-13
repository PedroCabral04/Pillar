using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Sales;
using erp.Models.Sales;
using erp.Models.Financial;
using erp.Mappings;
using erp.Services.Financial;

namespace erp.Services.Sales;

public class SalesService : ISalesService
{
    private readonly ApplicationDbContext _context;
    private readonly SalesMapper _mapper;
    private readonly ILogger<SalesService> _logger;
    private readonly ICommissionService? _commissionService;

    public SalesService(
        ApplicationDbContext context,
        SalesMapper mapper,
        ILogger<SalesService> logger,
        ICommissionService? commissionService = null)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _commissionService = commissionService;
    }

    public async Task<SaleDto> CreateAsync(CreateSaleDto dto, int userId, CancellationToken ct = default)
    {
        if (!dto.Items.Any())
        {
            throw new InvalidOperationException("A venda deve conter pelo menos um item");
        }

        const int maxRetries = 3;
        int retryCount = 0;

        while (true)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate sale number
                var saleNumber = await GenerateSaleNumberAsync();

                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    CustomerId = dto.CustomerId,
                    UserId = userId,
                    SaleDate = dto.SaleDate,
                    DiscountAmount = dto.DiscountAmount,
                    Status = dto.Status,
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                decimal totalAmount = 0;

                foreach (var itemDto in dto.Items)
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, ct);

                    if (product == null)
                    {
                        throw new InvalidOperationException($"Produto com ID {itemDto.ProductId} não encontrado");
                    }

                    // Se a venda estiver sendo criada já como FINALIZADA, damos baixa no estoque
                    if (SaleStatus.IsFinalized(sale.Status))
                    {
                        if (product.CurrentStock < itemDto.Quantity && !product.AllowNegativeStock)
                        {
                            throw new InvalidOperationException(
                                $"Estoque insuficiente para o produto '{product.Name}' (SKU: {product.Sku}). " +
                                $"Disponível: {product.CurrentStock}, Solicitado: {itemDto.Quantity}");
                        }
                        
                        var previousStock = product.CurrentStock;
                        product.CurrentStock -= itemDto.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;

                        // Criar movimentação de estoque
                        var stockMovement = new Models.Inventory.StockMovement
                        {
                            ProductId = itemDto.ProductId,
                            Type = Models.Inventory.MovementType.Out,
                            Reason = Models.Inventory.MovementReason.Sale,
                            Quantity = itemDto.Quantity,
                            PreviousStock = previousStock,
                            CurrentStock = product.CurrentStock,
                            UnitCost = product.CostPrice,
                            TotalCost = product.CostPrice * itemDto.Quantity,
                            Notes = $"Baixa automática de estoque - Venda {saleNumber}",
                            MovementDate = DateTime.UtcNow,
                            CreatedByUserId = userId
                        };
                        _context.StockMovements.Add(stockMovement);
                    }

                    var itemTotal = (itemDto.Quantity * itemDto.UnitPrice) - itemDto.Discount;
                    totalAmount += itemTotal;

                    var item = new SaleItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        CostPrice = product.CostPrice,
                        Discount = itemDto.Discount,
                        Total = itemTotal
                    };

                    sale.Items.Add(item);
                }

                sale.TotalAmount = totalAmount;
                sale.NetAmount = totalAmount - sale.DiscountAmount;

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                await EnsureFinancialReceivableForSaleAsync(sale);
                await transaction.CommitAsync();

                // Calcular comissões se finalizada
                if (_commissionService != null && SaleStatus.IsFinalized(sale.Status))
                {
                    try { await _commissionService.CalculateCommissionsForSaleAsync(sale.Id); }
                    catch (Exception ex) { _logger.LogError(ex, "Erro ao calcular comissões para venda {SaleId}", sale.Id); }
                }

                return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Erro ao recuperar venda criada");
            }
            catch (DbUpdateException ex) when (retryCount < maxRetries && (ex.InnerException?.Message.Contains("duplicate key") == true || ex is DbUpdateConcurrencyException))
            {
                await transaction.RollbackAsync();
                retryCount++;
                _logger.LogWarning("Concorrência ou duplicidade detectada ao criar venda. Tentativa {RetryCount} de {MaxRetries}", retryCount, maxRetries);
                
                // Limpar o contexto para evitar problemas com entidades rastreadas
                _context.ChangeTracker.Clear();
                
                // Pequeno delay aleatório para evitar colisões sucessivas
                await Task.Delay(new Random().Next(50, 200));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<SaleDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sale == null)
        {
            return null;
        }

        var dto = _mapper.ToDtoWithRelations(sale);
        dto.Items = sale.Items.Select(i => _mapper.ToDtoWithProduct(i)).ToList();

        return dto;
    }

    public async Task<(List<SaleDto> items, int total)> SearchAsync(
        string? search,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        int? customerId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.SaleNumber.Contains(search) ||
                (s.Customer != null && s.Customer.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.SaleDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.SaleDate <= endDate.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(ct);

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = sales.Select(s => _mapper.ToDtoWithRelations(s)).ToList();
        
        return (dtos, total);
    }

    public async Task<SaleDto> UpdateAsync(int id, UpdateSaleDto dto, CancellationToken ct = default)
    {
        var sale = await _context.Sales
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sale == null)
        {
            throw new KeyNotFoundException($"Venda com ID {id} não encontrada");
        }

        if (SaleStatus.IsFinalized(sale.Status))
        {
            throw new InvalidOperationException("Não é possível editar uma venda finalizada");
        }

        if (SaleStatus.IsCancelled(sale.Status))
        {
            throw new InvalidOperationException("Não é possível editar uma venda cancelada");
        }

        sale.CustomerId = dto.CustomerId;
        sale.DiscountAmount = dto.DiscountAmount;
        sale.Status = dto.Status;
        sale.PaymentMethod = dto.PaymentMethod;
        sale.Notes = dto.Notes;
        sale.NetAmount = sale.TotalAmount - sale.DiscountAmount;
        sale.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Erro ao recuperar venda atualizada");
    }

    public async Task<bool> CancelAsync(int id, CancellationToken ct = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return false;
            }

            if (SaleStatus.IsCancelled(sale.Status))
            {
                throw new InvalidOperationException("Venda já está cancelada");
            }

            var wasFinalized = SaleStatus.IsFinalized(sale.Status);

            // Se a venda estava finalizada, devolver o estoque
            if (wasFinalized)
            {
                foreach (var item in sale.Items)
                {
                    var previousStock = item.Product.CurrentStock;
                    
                    // Devolver quantidade ao estoque
                    item.Product.CurrentStock += item.Quantity;
                    item.Product.UpdatedAt = DateTime.UtcNow;

                    // Criar movimentação de estoque (entrada por devolução)
                    var stockMovement = new Models.Inventory.StockMovement
                    {
                        ProductId = item.ProductId,
                        Type = Models.Inventory.MovementType.In,
                        Reason = Models.Inventory.MovementReason.Adjustment,
                        Quantity = item.Quantity,
                        PreviousStock = previousStock,
                        CurrentStock = item.Product.CurrentStock,
                        UnitCost = item.Product.CostPrice,
                        TotalCost = item.Product.CostPrice * item.Quantity,
                        DocumentNumber = sale.SaleNumber,
                        SaleOrderId = sale.Id,
                        Notes = $"Devolução de estoque - Cancelamento da venda {sale.SaleNumber}",
                        MovementDate = DateTime.UtcNow,
                        CreatedByUserId = sale.UserId
                    };

                    _context.StockMovements.Add(stockMovement);
                }

                _logger.LogInformation(
                    "Estoque devolvido para venda {SaleNumber}. {ItemCount} itens processados.",
                    sale.SaleNumber, sale.Items.Count);
            }

            sale.Status = "Cancelada";
            sale.UpdatedAt = DateTime.UtcNow;

            await CancelFinancialReceivableForSaleAsync(sale);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Cancel commissions when sale is cancelled
            if (_commissionService != null && wasFinalized)
            {
                try
                {
                    await _commissionService.CancelCommissionsForSaleAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao cancelar comissões para venda {SaleId}", id);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao cancelar venda {SaleId}", id);
            throw;
        }
    }

    public async Task<SaleDto> FinalizeAsync(int id, CancellationToken ct = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                throw new KeyNotFoundException($"Venda com ID {id} não encontrada");
            }

            if (SaleStatus.IsFinalized(sale.Status))
            {
                throw new InvalidOperationException("Venda já está finalizada");
            }

            if (SaleStatus.IsCancelled(sale.Status))
            {
                throw new InvalidOperationException("Não é possível finalizar uma venda cancelada");
            }

            // Processar baixa de estoque para cada item
            foreach (var item in sale.Items)
            {
                // Validar estoque disponível
                if (item.Product.CurrentStock < item.Quantity && !item.Product.AllowNegativeStock)
                {
                    throw new InvalidOperationException(
                        $"Estoque insuficiente para o produto '{item.Product.Name}' (SKU: {item.Product.Sku}). " +
                        $"Disponível: {item.Product.CurrentStock}, Solicitado: {item.Quantity}");
                }

                var previousStock = item.Product.CurrentStock;
                
                // Atualizar estoque do produto
                item.Product.CurrentStock -= item.Quantity;
                item.Product.UpdatedAt = DateTime.UtcNow;

                // Criar movimentação de estoque
                var stockMovement = new Models.Inventory.StockMovement
                {
                    ProductId = item.ProductId,
                    Type = Models.Inventory.MovementType.Out,
                    Reason = Models.Inventory.MovementReason.Sale,
                    Quantity = item.Quantity,
                    PreviousStock = previousStock,
                    CurrentStock = item.Product.CurrentStock,
                    UnitCost = item.Product.CostPrice,
                    TotalCost = item.Product.CostPrice * item.Quantity,
                    DocumentNumber = sale.SaleNumber,
                    SaleOrderId = sale.Id,
                    Notes = $"Baixa automática de estoque - Venda {sale.SaleNumber}",
                    MovementDate = DateTime.UtcNow,
                    CreatedByUserId = sale.UserId
                };

                _context.StockMovements.Add(stockMovement);
            }

            sale.Status = "Finalizada";
            sale.UpdatedAt = DateTime.UtcNow;

            await EnsureFinancialReceivableForSaleAsync(sale);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Calculate commissions after sale is finalized
            if (_commissionService != null)
            {
                try
                {
                    await _commissionService.CalculateCommissionsForSaleAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular comissões para venda {SaleId}", id);
                }
            }

            _logger.LogInformation(
                "Venda {SaleNumber} finalizada com sucesso. {ItemCount} itens processados.",
                sale.SaleNumber, sale.Items.Count);

            return await GetByIdAsync(id) ?? throw new InvalidOperationException("Erro ao recuperar venda finalizada");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao finalizar venda {SaleId}", id);
            throw;
        }
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _context.Sales
            .AsNoTracking()
            .Where(s => SaleStatus.IsFinalized(s.Status) &&
                       s.SaleDate >= startDate &&
                       s.SaleDate <= endDate)
            .SumAsync(s => s.NetAmount, ct);
    }

    public async Task<List<(string productName, decimal quantity)>> GetTopProductsAsync(
        int topN,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        return await _context.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.Product)
            .Where(i => SaleStatus.IsFinalized(i.Sale.Status) &&
                       i.Sale.SaleDate >= startDate &&
                       i.Sale.SaleDate <= endDate)
            .GroupBy(i => new { i.Product.Name })
            .Select(g => new { ProductName = g.Key.Name, Quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Quantity)
            .Take(topN)
            .Select(x => ValueTuple.Create(x.ProductName, x.Quantity))
            .ToListAsync(ct);
    }

    private async Task<string> GenerateSaleNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"VEN{today:yyyyMMdd}";
        
        var lastSale = await _context.Sales
            .Where(s => s.SaleNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SaleNumber)
            .FirstOrDefaultAsync();

        if (lastSale == null)
        {
            return $"{prefix}001";
        }

        var lastNumber = int.Parse(lastSale.SaleNumber.Substring(prefix.Length));
        return $"{prefix}{(lastNumber + 1):D3}";
    }

    private async Task EnsureFinancialReceivableForSaleAsync(Sale sale)
    {
        if (!string.Equals(sale.Status, "Finalizada", StringComparison.OrdinalIgnoreCase) || !sale.CustomerId.HasValue)
        {
            return;
        }

        var existing = await _context.AccountsReceivable
            .FirstOrDefaultAsync(a => a.InvoiceNumber == sale.SaleNumber && a.CustomerId == sale.CustomerId.Value);

        if (existing != null)
        {
            return;
        }

        var method = PaymentMethodResolver.FromSaleText(sale.PaymentMethod);
        var isPaid = PaymentMethodResolver.IsImmediatelyPaid(method);
        var now = DateTime.UtcNow;

        var receivable = new AccountReceivable
        {
            TenantId = sale.TenantId,
            CustomerId = sale.CustomerId.Value,
            InvoiceNumber = sale.SaleNumber,
            OriginalAmount = sale.NetAmount,
            DiscountAmount = 0,
            InterestAmount = 0,
            FineAmount = 0,
            PaidAmount = isPaid ? sale.NetAmount : 0,
            IssueDate = sale.SaleDate,
            DueDate = sale.SaleDate,
            PaymentDate = isPaid ? now : null,
            Status = isPaid ? AccountStatus.Paid : AccountStatus.Pending,
            PaymentMethod = method,
            Notes = $"Gerado automaticamente pela venda {sale.SaleNumber}",
            CreatedAt = now,
            CreatedByUserId = sale.UserId,
            ReceivedByUserId = isPaid ? sale.UserId : null
        };

        _context.AccountsReceivable.Add(receivable);
    }

    private async Task CancelFinancialReceivableForSaleAsync(Sale sale)
    {
        var receivable = await _context.AccountsReceivable
            .FirstOrDefaultAsync(a => a.InvoiceNumber == sale.SaleNumber && a.CustomerId == sale.CustomerId);

        if (receivable == null)
        {
            return;
        }

        receivable.Status = AccountStatus.Cancelled;
        receivable.UpdatedAt = DateTime.UtcNow;
    }
}
