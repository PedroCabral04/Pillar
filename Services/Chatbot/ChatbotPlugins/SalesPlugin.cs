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
    private readonly IChatbotCacheService _cacheService;
    private const string PluginName = "SalesPlugin";

    public SalesPlugin(ISalesService salesService, IInventoryService inventoryService, IChatbotCacheService cacheService)
    {
        _salesService = salesService;
        _inventoryService = inventoryService;
        _cacheService = cacheService;
    }

    [KernelFunction, Description("Lista as vendas recentes")]
    public async Task<string> ListRecentSales(
        [Description("Quantidade de vendas a listar (padrão: 10)")] int limit = 10)
    {
        try
        {
            // Tentar obter do cache
            var cacheKey = $"limit:{limit}";
            var cachedResult = _cacheService.GetPluginData<string>(PluginName, nameof(ListRecentSales), cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

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
                return "📊 Não há vendas registradas no momento.";
            }

            var salesList = result.items.Select(s => 
                $"| #{s.Id} | {s.CreatedAt:dd/MM/yyyy} | R$ {s.TotalAmount:N2} | {s.Status} |"
            );

            var remaining = result.total - limit;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} vendas.*" : "";

            var response = $"""
                🛒 **Vendas Recentes** ({result.total} total)
                
                | Venda | Data | Total | Status |
                |-------|------|-------|--------|
                {string.Join("\n", salesList)}
                {moreText}
                """;
            
            // Armazenar no cache
            _cacheService.SetPluginData(PluginName, nameof(ListRecentSales), response, cacheKey);
            
            return response;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar vendas: {ex.Message}";
        }
    }

    [KernelFunction, Description("Cria uma nova venda/pedido no sistema. Campos obrigatórios: SKU do produto e quantidade. Campos opcionais: ID do cliente, método de pagamento, desconto, observações.")]
    public async Task<string> CreateSale(
        [Description("SKU do produto (obrigatório)")] string productSku,
        [Description("Quantidade do produto (obrigatório)")] int quantity,
        [Description("ID do cliente (opcional, deixe vazio para venda sem cliente)")] int? customerId = null,
        [Description("Método de pagamento (opcional). Exemplos: Dinheiro, Cartão de Crédito, Cartão de Débito, PIX, Boleto")] string? paymentMethod = null,
        [Description("Desconto em reais a aplicar no total (opcional, padrão: 0)")] decimal discountAmount = 0,
        [Description("Observações adicionais (opcional)")] string? notes = null)
    {
        try
        {
            // Validar quantidade
            if (quantity <= 0)
            {
                return "❌ A quantidade deve ser maior que zero.";
            }

            // Validar desconto
            if (discountAmount < 0)
            {
                return "❌ O desconto não pode ser negativo.";
            }

            // Buscar produto
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto
            {
                SearchTerm = productSku,
                PageSize = 5
            });

            var product = result.Products.FirstOrDefault();

            if (product == null)
            {
                return $"❌ Produto com SKU **'{productSku}'** não encontrado.";
            }

            // Verificar estoque
            if (product.CurrentStock < quantity)
            {
                return $"""
                    ❌ **Estoque Insuficiente!**
                    
                    - **Produto:** {product.Name}
                    - **Solicitado:** {quantity} un.
                    - **Disponível:** {product.CurrentStock} un.
                    """;
            }

            // Calcular subtotal e verificar desconto
            var subtotal = product.SalePrice * quantity;
            if (discountAmount > subtotal)
            {
                return $"❌ O desconto (R$ {discountAmount:N2}) não pode ser maior que o subtotal (R$ {subtotal:N2}).";
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
                CustomerId = customerId,
                PaymentMethod = paymentMethod,
                DiscountAmount = discountAmount,
                Notes = notes,
                SaleDate = DateTime.Now,
                Status = "Pendente"
            };

            var createdSale = await _salesService.CreateAsync(saleDto, 1);

            // Invalidar cache após criar venda
            _cacheService.InvalidatePluginCache(PluginName);
            _cacheService.InvalidatePluginCache("ProductsPlugin"); // Estoque mudou

            var customerInfo = customerId.HasValue ? $"Cliente #{customerId}" : "Venda sem cliente";
            var paymentInfo = !string.IsNullOrEmpty(paymentMethod) ? paymentMethod : "Não informado";
            var discountInfo = discountAmount > 0 ? $"R$ {discountAmount:N2}" : "—";

            return $"""
                ✅ **Venda Registrada!**
                
                | Campo | Valor |
                |-------|-------|
                | **Venda** | #{createdSale.Id} |
                | **Cliente** | {customerInfo} |
                | **Produto** | {product.Name} |
                | **Quantidade** | {quantity} un. |
                | **Preço Unitário** | R$ {product.SalePrice:N2} |
                | **Subtotal** | R$ {subtotal:N2} |
                | **Desconto** | {discountInfo} |
                | **Total** | R$ {createdSale.TotalAmount:N2} |
                | **Pagamento** | {paymentInfo} |
                | **Status** | {createdSale.Status} |
                """;
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
                return $"🔍 Venda **#{saleId}** não encontrada.";
            }

            var itemsTable = sale.Items.Select(item =>
                $"| {item.Quantity}x | {item.ProductName} | R$ {item.UnitPrice:N2} | R$ {item.Total:N2} |"
            );

            var notesSection = string.IsNullOrEmpty(sale.Notes) ? "" : $"\n\n> **Obs:** {sale.Notes}";

            return $"""
                📋 **Venda #{sale.Id}**
                
                - **Data:** {sale.CreatedAt:dd/MM/yyyy HH:mm}
                - **Status:** {sale.Status}
                
                **Itens:**
                
                | Qtd | Produto | Unit. | Subtotal |
                |-----|---------|-------|----------|
                {string.Join("\n", itemsTable)}
                
                ---
                💰 **Total: R$ {sale.TotalAmount:N2}**{notesSection}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar venda: {ex.Message}";
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
                return "❌ Data inicial inválida. Use o formato: `yyyy-MM-dd`";
            }

            if (!DateTime.TryParse(endDate, out var end))
            {
                return "❌ Data final inválida. Use o formato: `yyyy-MM-dd`";
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
                return $"📊 Nenhuma venda entre **{start:dd/MM/yyyy}** e **{end:dd/MM/yyyy}**.";
            }

            var average = total / count;

            return $"""
                📊 **Resumo de Vendas**
                
                | Métrica | Valor |
                |---------|-------|
                | **Período** | {start:dd/MM/yyyy} a {end:dd/MM/yyyy} |
                | **Quantidade** | {count} vendas |
                | **Total** | R$ {total:N2} |
                | **Ticket médio** | R$ {average:N2} |
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao calcular total: {ex.Message}";
        }
    }
}
