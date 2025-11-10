using erp.Data;
using erp.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Reports;

public class InventoryReportService : IInventoryReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryReportService> _logger;

    public InventoryReportService(ApplicationDbContext context, ILogger<InventoryReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StockLevelsReportDto> GenerateStockLevelsReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (filter.ProductId.HasValue)
            {
                query = query.Where(p => p.Id == filter.ProductId.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.OnlyLowStock == true)
            {
                query = query.Where(p => p.CurrentStock <= p.MinimumStock);
            }

            var products = await query.ToListAsync();

            var items = products.Select(p =>
            {
                var status = "OK";
                if (p.CurrentStock == 0)
                    status = "Sem Estoque";
                else if (p.CurrentStock <= p.MinimumStock)
                    status = "Estoque Baixo";
                else if (p.CurrentStock <= (p.MinimumStock * 1.2m))
                    status = "Atenção";

                return new StockLevelItemDto
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    ProductName = p.Name,
                    Category = p.Category?.Name ?? "Sem categoria",
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    MaximumStock = p.MaximumStock,
                    Unit = p.Unit,
                    CostPrice = p.CostPrice,
                    SalePrice = p.SalePrice,
                    TotalValue = p.CurrentStock * p.CostPrice,
                    Status = status
                };
            }).ToList();

            var summary = new StockLevelsSummaryDto
            {
                TotalProducts = items.Count,
                ProductsInStock = items.Count(i => i.CurrentStock > i.MinimumStock),
                ProductsLowStock = items.Count(i => i.CurrentStock > 0 && i.CurrentStock <= i.MinimumStock),
                ProductsOutOfStock = items.Count(i => i.CurrentStock == 0),
                TotalInventoryValue = items.Sum(i => i.TotalValue)
            };

            return new StockLevelsReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de níveis de estoque");
            throw;
        }
    }

    public async Task<StockMovementReportDto> GenerateStockMovementReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            var query = _context.StockMovements
                .Include(m => m.Product)
                .Include(m => m.CreatedByUser)
                .AsQueryable();

            if (filter.StartDate.HasValue)
            {
                query = query.Where(m => m.MovementDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(m => m.MovementDate <= filter.EndDate.Value);
            }

            if (filter.ProductId.HasValue)
            {
                query = query.Where(m => m.ProductId == filter.ProductId.Value);
            }

            if (!string.IsNullOrEmpty(filter.MovementType))
            {
                query = query.Where(m => m.Type.ToString() == filter.MovementType);
            }

            var movements = await query.OrderByDescending(m => m.MovementDate).ToListAsync();

            var items = movements.Select(m => new StockMovementItemDto
            {
                MovementId = m.Id,
                Date = m.MovementDate,
                ProductName = m.Product?.Name ?? "N/A",
                Sku = m.Product?.Sku ?? "N/A",
                Type = m.Type.ToString(),
                Reason = m.Reason.ToString(),
                Quantity = m.Quantity,
                Unit = m.Product?.Unit ?? "UN",
                UnitCost = m.UnitCost,
                TotalCost = m.Quantity * m.UnitCost,
                ReferenceDocument = m.DocumentNumber,
                UserName = m.CreatedByUser?.FullName ?? m.CreatedByUser?.UserName ?? "N/A"
            }).ToList();

            var summary = new StockMovementSummaryDto
            {
                TotalMovements = items.Count,
                MovementsByType = items.GroupBy(i => i.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TotalValueMovements = items.Sum(i => Math.Abs(i.TotalCost))
            };

            return new StockMovementReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de movimentação de estoque");
            throw;
        }
    }

    public async Task<InventoryValuationReportDto> GenerateInventoryValuationReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.CurrentStock > 0)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            var products = await query.ToListAsync();

            var items = products
                .GroupBy(p => p.Category?.Name ?? "Sem categoria")
                .Select(g =>
                {
                    var totalCostValue = g.Sum(p => p.CurrentStock * p.CostPrice);
                    var totalSaleValue = g.Sum(p => p.CurrentStock * p.SalePrice);
                    var potentialProfit = totalSaleValue - totalCostValue;

                    return new InventoryValuationItemDto
                    {
                        Category = g.Key,
                        ProductCount = g.Count(),
                        TotalQuantity = g.Sum(p => p.CurrentStock),
                        TotalCostValue = totalCostValue,
                        TotalSaleValue = totalSaleValue,
                        PotentialProfit = potentialProfit,
                        ProfitMargin = totalCostValue > 0 ? (potentialProfit / totalCostValue * 100) : 0
                    };
                })
                .OrderByDescending(i => i.TotalCostValue)
                .ToList();

            var summary = new InventoryValuationSummaryDto
            {
                TotalCostValue = items.Sum(i => i.TotalCostValue),
                TotalSaleValue = items.Sum(i => i.TotalSaleValue),
                TotalPotentialProfit = items.Sum(i => i.PotentialProfit),
                AverageProfitMargin = items.Any() ? items.Average(i => i.ProfitMargin) : 0
            };

            return new InventoryValuationReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de avaliação de estoque");
            throw;
        }
    }
}
