using erp.Data;
using erp.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Reports;

public class SalesReportService : ISalesReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SalesReportService> _logger;

    public SalesReportService(ApplicationDbContext context, ILogger<SalesReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SalesReportResultDto> GenerateSalesReportAsync(SalesReportFilterDto filter)
    {
        try
        {
            var salesQuery = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Items)
                .AsNoTracking()
                .AsQueryable();

            ApplyCommonFilters(ref salesQuery, filter);

            var sales = await salesQuery.ToListAsync();

            var items = sales.Select(s => new SalesReportItemDto
            {
                SaleId = s.Id,
                SaleNumber = s.SaleNumber,
                SaleDate = s.SaleDate,
                CustomerName = s.Customer?.Name ?? "Cliente não informado",
                SalespersonName = s.User?.FullName ?? s.User?.UserName ?? "N/A",
                TotalAmount = s.TotalAmount,
                DiscountAmount = s.DiscountAmount,
                NetAmount = s.NetAmount,
                Status = s.Status,
                PaymentMethod = s.PaymentMethod,
                ItemCount = s.Items.Count
            }).ToList();

            var summary = new SalesReportSummaryDto
            {
                TotalSales = items.Count,
                TotalRevenue = items.Sum(i => i.TotalAmount),
                TotalDiscounts = items.Sum(i => i.DiscountAmount),
                NetRevenue = items.Sum(i => i.NetAmount),
                AverageTicket = items.Any() ? items.Average(i => i.TotalAmount) : 0,
                TotalItemsSold = items.Sum(i => i.ItemCount),
                SalesByStatus = items.GroupBy(i => i.Status).ToDictionary(g => g.Key, g => g.Count()),
                RevenueByPaymentMethod = items
                    .Where(i => !string.IsNullOrEmpty(i.PaymentMethod))
                    .GroupBy(i => i.PaymentMethod!)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.NetAmount))
            };

            return new SalesReportResultDto
            {
                Items = items,
                Summary = summary,
                Sales = sales
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de vendas");
            throw;
        }
    }

    public async Task<CustomerSalesReportResultDto> GenerateByCustomerReportAsync(SalesReportFilterDto filter)
    {
        try
        {
            var query = _context.Sales
                .Include(s => s.Customer)
                .AsNoTracking()
                .AsQueryable();

            ApplyCommonFilters(ref query, filter);

            var grouped = await query
                .GroupBy(s => new { s.CustomerId, CustomerName = s.Customer != null ? s.Customer.Name : "Cliente não informado" })
                .Select(g => new CustomerSalesReportItemDto
                {
                    CustomerId = g.Key.CustomerId ?? 0,
                    CustomerName = g.Key.CustomerName,
                    TotalSales = g.Count(),
                    TotalAmount = g.Sum(s => s.TotalAmount)
                })
                .ToListAsync();

            return new CustomerSalesReportResultDto
            {
                TotalCustomers = grouped.Count,
                Customers = grouped
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório por cliente");
            throw;
        }
    }

    public async Task<ProductSalesReportResultDto> GenerateByProductReportAsync(SalesReportFilterDto filter)
    {
        try
        {
            var itemsQuery = _context.SaleItems
                .Include(i => i.Product)
                .Include(i => i.Sale)
                .AsNoTracking()
                .AsQueryable();

            // Apply filters based on owning sale
            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                itemsQuery = itemsQuery.Where(i => i.Sale.SaleDate >= startDate);
            }
            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                itemsQuery = itemsQuery.Where(i => i.Sale.SaleDate <= endDate);
            }
            if (filter.CustomerId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.Sale.CustomerId == filter.CustomerId.Value);
            if (filter.SalespersonId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.Sale.UserId == filter.SalespersonId.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                itemsQuery = itemsQuery.Where(i => i.Sale.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.PaymentMethod))
                itemsQuery = itemsQuery.Where(i => i.Sale.PaymentMethod == filter.PaymentMethod);
            if (filter.ProductId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.ProductId == filter.ProductId.Value);

            var grouped = await itemsQuery
                .GroupBy(i => new { i.ProductId, ProductName = i.Product.Name })
                .Select(g => new ProductSalesReportItemDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = (int)g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Total)
                })
                .ToListAsync();

            return new ProductSalesReportResultDto
            {
                TotalProducts = grouped.Count,
                Products = grouped
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório por produto");
            throw;
        }
    }

    public async Task<PaymentMethodSalesReportResultDto> GenerateByPaymentMethodReportAsync(SalesReportFilterDto filter)
    {
        try
        {
            var salesQuery = _context.Sales.AsNoTracking().AsQueryable();
            ApplyCommonFilters(ref salesQuery, filter);

            var grouped = await salesQuery
                .Where(s => !string.IsNullOrEmpty(s.PaymentMethod))
                .GroupBy(s => s.PaymentMethod!)
                .Select(g => new PaymentMethodSalesReportItemDto
                {
                    PaymentMethod = g.Key,
                    TotalSales = g.Count(),
                    TotalAmount = g.Sum(s => s.TotalAmount)
                })
                .ToListAsync();

            return new PaymentMethodSalesReportResultDto
            {
                TotalMethods = grouped.Count,
                PaymentMethods = grouped
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório por método de pagamento");
            throw;
        }
    }

    private void ApplyCommonFilters(ref IQueryable<erp.Models.Sales.Sale> query, SalesReportFilterDto filter)
    {
        if (filter.StartDate.HasValue)
        {
            var startDate = filter.StartDate.Value.ToUniversalTime();
            query = query.Where(s => s.SaleDate >= startDate);
        }
        if (filter.EndDate.HasValue)
        {
            var endDate = filter.EndDate.Value.ToUniversalTime();
            query = query.Where(s => s.SaleDate <= endDate);
        }
        if (filter.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == filter.CustomerId.Value);
        if (filter.SalespersonId.HasValue)
            query = query.Where(s => s.UserId == filter.SalespersonId.Value);
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(s => s.Status == filter.Status);
        if (!string.IsNullOrEmpty(filter.PaymentMethod))
            query = query.Where(s => s.PaymentMethod == filter.PaymentMethod);
        // ProductId only applies in product-specific report
    }

    public async Task<SalesHeatmapReportDto> GenerateSalesHeatmapAsync(SalesReportFilterDto filter)
    {
        try
        {
            var salesQuery = _context.Sales
                .Where(s => s.Status != "Cancelada")
                .AsNoTracking()
                .AsQueryable();

            ApplyCommonFilters(ref salesQuery, filter);

            var sales = await salesQuery
                .Select(s => new { s.SaleDate, s.TotalAmount })
                .ToListAsync();

            // Brazilian Portuguese day names
            var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Sunday, "Domingo" },
                { DayOfWeek.Monday, "Segunda" },
                { DayOfWeek.Tuesday, "Terça" },
                { DayOfWeek.Wednesday, "Quarta" },
                { DayOfWeek.Thursday, "Quinta" },
                { DayOfWeek.Friday, "Sexta" },
                { DayOfWeek.Saturday, "Sábado" }
            };

            // Group by day of week and hour
            var groupedData = sales
                .GroupBy(s => new { DayOfWeek = s.SaleDate.DayOfWeek, Hour = s.SaleDate.Hour })
                .Select(g => new
                {
                    g.Key.DayOfWeek,
                    g.Key.Hour,
                    SalesCount = g.Count(),
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToList();

            // Build heatmap series (one per day, starting from Monday)
            var series = new List<SalesHeatmapSeriesDto>();
            var orderedDays = new[] 
            { 
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday 
            };

            foreach (var day in orderedDays)
            {
                var daySeries = new SalesHeatmapSeriesDto
                {
                    Name = dayNames[day],
                    Data = new List<SalesHeatmapDataPoint>()
                };

                // Create data points for each hour (0-23)
                for (int hour = 0; hour < 24; hour++)
                {
                    var hourData = groupedData.FirstOrDefault(g => g.DayOfWeek == day && g.Hour == hour);
                    daySeries.Data.Add(new SalesHeatmapDataPoint
                    {
                        X = $"{hour:D2}:00",
                        Y = hourData?.SalesCount ?? 0,
                        Revenue = hourData?.Revenue ?? 0
                    });
                }

                series.Add(daySeries);
            }

            // Calculate summary
            var peakData = groupedData.OrderByDescending(g => g.SalesCount).FirstOrDefault();
            var lowestData = groupedData.Where(g => g.SalesCount > 0).OrderBy(g => g.SalesCount).FirstOrDefault();

            var summary = new SalesHeatmapSummaryDto
            {
                TotalSales = sales.Count,
                TotalRevenue = sales.Sum(s => s.TotalAmount),
                PeakDay = peakData != null ? dayNames[peakData.DayOfWeek] : "-",
                PeakHour = peakData != null ? $"{peakData.Hour:D2}:00" : "-",
                PeakSalesCount = peakData?.SalesCount ?? 0,
                PeakRevenue = peakData?.Revenue ?? 0,
                LowestDay = lowestData != null ? dayNames[lowestData.DayOfWeek] : "-",
                LowestHour = lowestData != null ? $"{lowestData.Hour:D2}:00" : "-",
                AverageSalesPerHour = groupedData.Any() ? (decimal)groupedData.Average(g => g.SalesCount) : 0
            };

            return new SalesHeatmapReportDto
            {
                Series = series,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar mapa de calor de vendas");
            throw;
        }
    }
}
