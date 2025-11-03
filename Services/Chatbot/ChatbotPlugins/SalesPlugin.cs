using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Sales;
using erp.Services.Inventory;
using erp.DTOs.Sales;
using erp.DTOs.Inventory;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para gerenciar vendas através do chatbot
/// </summary>
public class SalesPlugin
{
    private readonly ISalesService _salesService;
    private readonly IInventoryService _inventoryService;

    public SalesPlugin(ISalesService salesService, IInventoryService inventoryService)
    {
        _salesService = salesService;
        _inventoryService = inventoryService;
    }

    [KernelFunction, Description("Lista as vendas recentes")]
    public async Task<string> ListRecentSales(
        [Description("Quantidade de vendas a listar (padrão: 10)")] int limit = 10)
    {
        try
        {
            var result = await _salesService.SearchAsync(
                search: null,
                status: null,
                startDate: null,
                endDate: null,
                customerId: null,
                page: 1,
                pageSize: limit);

            if (!result.items.Any())
            {
                return "Não há vendas registradas no momento.";
            }

            var salesList = result.items.Select(s => 
                $"- Venda #{s.Id} ({s.CreatedAt:dd/MM/yyyy}) - " +
                $"Total: R$ {s.TotalAmount:F2} - " +
                $"Status: {s.Status}"
            );

            return $"Vendas recentes:\n{string.Join("\n", salesList)}";
        }
        catch (Exception ex)
        {
            return $"Erro ao listar vendas: {ex.Message}";
        }
    }

    [KernelFunction, Description("Cria uma nova venda/pedido no sistema")]
    public async Task<string> CreateSale(
        [Description("SKU do produto")] string productSku,
        [Description("Quantidade do produto")] int quantity,
        [Description("Observações adicionais (opcional)")] string? notes = null)
    {
        try
        {
            // Buscar produto
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto
            {
                SearchTerm = productSku,
                PageSize = 5
            });

            var product = result.Products.FirstOrDefault();

            if (product == null)
            {
                return $"❌ Produto com SKU '{productSku}' não encontrado. " +
                       $"Por favor, verifique o código do produto.";
            }

            // Verificar estoque
            if (product.CurrentStock < quantity)
            {
                return $"❌ Estoque insuficiente!\n" +
                       $"Produto: {product.Name}\n" +
                       $"Solicitado: {quantity} unidades\n" +
                       $"Disponível: {product.CurrentStock} unidades";
            }

            var saleDto = new CreateSaleDto
            {
                Items = new List<CreateSaleItemDto>
                {
                    new()
                    {
                        ProductId = product.Id,
                        Quantity = quantity,
                        UnitPrice = product.SalePrice,
                        Discount = 0
                    }
                },
                Notes = notes,
                SaleDate = DateTime.Now,
                Status = "Pendente"
            };

            var createdSale = await _salesService.CreateAsync(saleDto, 1); // TODO: Obter userId do contexto

            return $"✅ Venda registrada com sucesso!\n" +
                   $"Venda: #{createdSale.Id}\n" +
                   $"Produto: {product.Name}\n" +
                   $"Quantidade: {quantity} unidades\n" +
                   $"Valor unitário: R$ {product.SalePrice:F2}\n" +
                   $"Total: R$ {createdSale.TotalAmount:F2}\n" +
                   $"Status: {createdSale.Status}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao criar venda: {ex.Message}";
        }
    }

    [KernelFunction, Description("Busca informações de uma venda específica")]
    public async Task<string> GetSaleDetails(
        [Description("ID da venda")] int saleId)
    {
        try
        {
            var sale = await _salesService.GetByIdAsync(saleId);

            if (sale == null)
            {
                return $"Venda #{saleId} não encontrada.";
            }

            var itemsList = sale.Items.Select(item =>
                $"  - {item.Quantity}x {item.ProductName} @ R$ {item.UnitPrice:F2} = R$ {item.Total:F2}"
            );

            return $"📋 Detalhes da Venda #{sale.Id}\n" +
                   $"Data: {sale.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                   $"Status: {sale.Status}\n" +
                   $"\nItens:\n{string.Join("\n", itemsList)}\n" +
                   $"\n💰 Total: R$ {sale.TotalAmount:F2}" +
                   (string.IsNullOrEmpty(sale.Notes) ? "" : $"\n\nObservações: {sale.Notes}");
        }
        catch (Exception ex)
        {
            return $"Erro ao buscar venda: {ex.Message}";
        }
    }

    [KernelFunction, Description("Calcula o total de vendas em um período")]
    public async Task<string> GetSalesTotal(
        [Description("Data inicial (formato: yyyy-MM-dd)")] string startDate,
        [Description("Data final (formato: yyyy-MM-dd)")] string endDate)
    {
        try
        {
            if (!DateTime.TryParse(startDate, out var start))
            {
                return "❌ Data inicial inválida. Use o formato: yyyy-MM-dd";
            }

            if (!DateTime.TryParse(endDate, out var end))
            {
                return "❌ Data final inválida. Use o formato: yyyy-MM-dd";
            }

            var total = await _salesService.GetTotalSalesAsync(start, end);
            
            var result = await _salesService.SearchAsync(
                search: null,
                status: null,
                startDate: start,
                endDate: end,
                customerId: null,
                page: 1,
                pageSize: 10000);

            var count = result.total;

            if (count == 0)
            {
                return $"Nenhuma venda encontrada entre {start:dd/MM/yyyy} e {end:dd/MM/yyyy}.";
            }

            var average = total / count;

            return $"📊 Resumo de Vendas\n" +
                   $"Período: {start:dd/MM/yyyy} a {end:dd/MM/yyyy}\n" +
                   $"Quantidade de vendas: {count}\n" +
                   $"Valor total: R$ {total:F2}\n" +
                   $"Ticket médio: R$ {average:F2}";
        }
        catch (Exception ex)
        {
            return $"Erro ao calcular total de vendas: {ex.Message}";
        }
    }
}
