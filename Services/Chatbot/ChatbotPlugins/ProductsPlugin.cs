using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Inventory;
using erp.DTOs.Inventory;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para gerenciar produtos através do chatbot
/// </summary>
public class ProductsPlugin
{
    private readonly IInventoryService _inventoryService;
    private readonly IChatbotCacheService _cacheService;
    private readonly IChatbotUserContext _userContext;
    private const string PluginName = "ProductsPlugin";

    public ProductsPlugin(IInventoryService inventoryService, IChatbotCacheService cacheService, IChatbotUserContext userContext)
    {
        _inventoryService = inventoryService;
        _cacheService = cacheService;
        _userContext = userContext;
    }

    [KernelFunction, Description("Lista todos os produtos cadastrados no sistema. Use página > 1 para ver mais produtos.")]
    public async Task<string> ListProducts(
        [Description("Número máximo de produtos a retornar por página")] int maxResults = 10,
        [Description("Número da página (1 = primeira página, 2 = próxima, etc)")] int page = 1)
    {
        try
        {
            // Tentar obter do cache
            var cacheKey = $"{maxResults}:{page}";
            var cachedResult = _cacheService.GetPluginData<string>(PluginName, nameof(ListProducts), cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var skip = (page - 1) * maxResults;
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto 
            { 
                PageSize = maxResults + skip // Busca até a página atual
            });
            
            if (!result.Products.Any())
            {
                return "📦 Não há produtos cadastrados no momento.";
            }

            var products = result.Products.Skip(skip).Take(maxResults);
            
            if (!products.Any())
            {
                return $"📦 Não há mais produtos. Total: {result.TotalCount} produtos.";
            }
            
            var productList = products.Select(p => 
                $"- **{p.Name}** (SKU: `{p.Sku}`) — R$ {p.SalePrice:N2} — Estoque: {p.CurrentStock} un."
            );

            var shown = skip + products.Count();
            var remaining = result.TotalCount - shown;
            
            var pageInfo = page > 1 ? $" (Página {page})" : "";
            var moreText = remaining > 0 
                ? $"\n\n*Exibindo {shown} de {result.TotalCount}. Peça \"listar produtos página {page + 1}\" para ver mais.*" 
                : "";

            var response = $"📦 **Produtos Cadastrados**{pageInfo} ({result.TotalCount} total)\n\n{string.Join("\n", productList)}{moreText}";
            
            // Armazenar no cache
            _cacheService.SetPluginData(PluginName, nameof(ListProducts), response, cacheKey);
            
            return response;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar produtos: {ex.Message}";
        }
    }

    [KernelFunction, Description("Busca um produto específico pelo nome ou SKU")]
    public async Task<string> SearchProduct(
        [Description("Nome ou SKU do produto a ser buscado")] string searchTerm)
    {
        try
        {
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto
            {
                SearchTerm = searchTerm,
                PageSize = 5
            });

            var product = result.Products.FirstOrDefault();

            if (product == null)
            {
                return $"🔍 Produto **'{searchTerm}'** não encontrado. Deseja cadastrá-lo?";
            }

            return $"""                
                📦 **Produto Encontrado**
                
                | Campo | Valor |
                |-------|-------|
                | **Nome** | {product.Name} |
                | **SKU** | `{product.Sku}` |
                | **Descrição** | {product.Description ?? "—"} |
                | **Preço** | R$ {product.SalePrice:N2} |
                | **Estoque** | {product.CurrentStock} unidades |
                | **Categoria** | {product.CategoryName ?? "—"} |
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar produto: {ex.Message}";
        }
    }

    [KernelFunction, Description("Cadastra um novo produto no sistema. Campos obrigatórios: nome, SKU, preço de custo e preço de venda. Campos opcionais: descrição, categoria (nome da categoria), unidade, quantidade inicial em estoque.")]
    public async Task<string> CreateProduct(
        [Description("Nome do produto (obrigatório)")] string name,
        [Description("SKU/Código do produto (obrigatório)")] string sku,
        [Description("Preço de custo do produto (obrigatório)")] decimal costPrice,
        [Description("Preço de venda do produto (obrigatório)")] decimal salePrice,
        [Description("Descrição do produto (opcional)")] string? description = null,
        [Description("Nome da categoria do produto (opcional, padrão: primeira categoria encontrada)")] string? category = null,
        [Description("Unidade de medida (opcional, padrão: UN). Exemplos: UN, KG, M, L, CX")] string unit = "UN",
        [Description("Quantidade inicial em estoque (opcional, padrão: 0)")] decimal initialQuantity = 0)
    {
        try
        {
            // Validar preços
            if (costPrice < 0)
                return "❌ O preço de custo deve ser maior ou igual a zero.";
            if (salePrice <= 0)
                return "❌ O preço de venda deve ser maior que zero.";

            // Buscar categoria por nome
            int categoryId = 1;
            string categoryName = "Padrão";
            
            if (!string.IsNullOrWhiteSpace(category))
            {
                var (categories, _) = await _inventoryService.GetCategoriesAsync(search: category, page: 1, pageSize: 10);
                var foundCategory = categories.FirstOrDefault(c => 
                    c.Name.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Contains(category, StringComparison.OrdinalIgnoreCase));
                
                if (foundCategory != null)
                {
                    categoryId = foundCategory.Id;
                    categoryName = foundCategory.Name;
                }
                else
                {
                    // Listar categorias disponíveis
                    var (allCategories, _) = await _inventoryService.GetCategoriesAsync(page: 1, pageSize: 20);
                    if (allCategories.Any())
                    {
                        var categoryList = string.Join(", ", allCategories.Select(c => $"`{c.Name}`"));
                        return $"❌ Categoria **'{category}'** não encontrada.\n\n📂 Categorias disponíveis: {categoryList}";
                    }
                }
            }
            else
            {
                // Usar primeira categoria disponível
                var (categories, _) = await _inventoryService.GetCategoriesAsync(page: 1, pageSize: 1);
                if (categories.Any())
                {
                    categoryId = categories.First().Id;
                    categoryName = categories.First().Name;
                }
            }

            var productDto = new CreateProductDto
            {
                Name = name,
                Sku = sku,
                Description = description,
                CostPrice = costPrice,
                SalePrice = salePrice,
                CategoryId = categoryId,
                Unit = unit,
                CurrentStock = initialQuantity
            };

            var currentUserId = _userContext.CurrentUserId
                ?? throw new InvalidOperationException("User context not set. Chatbot operations require a valid user context for audit purposes.");
            var createdProduct = await _inventoryService.CreateProductAsync(productDto, currentUserId);

            // Invalidar cache de listagem de produtos após criar novo
            _cacheService.InvalidatePluginCache(PluginName);

            var marginPercent = costPrice > 0 ? ((salePrice - costPrice) / costPrice * 100) : 0;

            return $"""
                ✅ **Produto Cadastrado com Sucesso!**
                
                | Campo | Valor |
                |-------|-------|
                | **Nome** | {createdProduct.Name} |
                | **SKU** | `{createdProduct.Sku}` |
                | **Categoria** | {categoryName} |
                | **Unidade** | {unit} |
                | **Preço de Custo** | R$ {costPrice:N2} |
                | **Preço de Venda** | R$ {createdProduct.SalePrice:N2} |
                | **Margem** | {marginPercent:N1}% |
                | **Estoque Inicial** | {createdProduct.CurrentStock} {unit} |
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao cadastrar produto: {ex.Message}";
        }
    }

    [KernelFunction, Description("Verifica a quantidade em estoque de um produto")]
    public async Task<string> CheckStock(
        [Description("Nome ou SKU do produto")] string productIdentifier)
    {
        try
        {
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto
            {
                SearchTerm = productIdentifier,
                PageSize = 5
            });

            var product = result.Products.FirstOrDefault();

            if (product == null)
            {
                return $"🔍 Produto **'{productIdentifier}'** não encontrado.";
            }

            var (icon, status) = product.CurrentStock switch
            {
                0 => ("🔴", "SEM ESTOQUE"),
                < 10 => ("🟡", "ESTOQUE BAIXO"),
                _ => ("🟢", "ESTOQUE OK")
            };

            return $"""
                {icon} **{status}**
                
                - **Produto:** {product.Name}
                - **Disponível:** {product.CurrentStock} unidades
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao verificar estoque: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista produtos com estoque baixo ou zerado")]
    public async Task<string> GetLowStockProducts()
    {
        try
        {
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto
            {
                LowStock = true,
                PageSize = 10
            });

            if (!result.Products.Any())
            {
                return "✅ Todos os produtos estão com níveis de estoque adequados.";
            }

            var items = result.Products.Select(p => 
                $"- **{p.Name}** (SKU: `{p.Sku}`) — Atual: {p.CurrentStock} (Mín: {p.MinimumStock})"
            );

            return $"""
                ⚠️ **Produtos com Estoque Baixo**
                
                {string.Join("\n", items)}
                
                *Total de {result.TotalCount} produtos precisando de reposição.*
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao verificar estoque baixo: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém estatísticas gerais do inventário (total de produtos, valor em estoque, etc)")]
    public async Task<string> GetInventoryStats()
    {
        try
        {
            var stats = await _inventoryService.GetProductStatisticsAsync();
            
            return $"""
                📊 **Estatísticas do Inventário**
                
                **Total de Produtos:** {stats.TotalProducts}
                **Valor Total em Estoque:** R$ {stats.TotalStockValue:N2}
                **Produtos com Estoque Baixo:** {stats.LowStockProducts}
                **Produtos Sem Estoque:** {stats.OutOfStockProducts}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao obter estatísticas: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista as categorias de produtos cadastradas")]
    public async Task<string> GetProductCategories()
    {
        try
        {
            var result = await _inventoryService.GetCategoriesAsync(pageSize: 50);
            
            if (!result.Categories.Any())
            {
                return "📂 Nenhuma categoria de produto cadastrada.";
            }

            var categories = result.Categories.Select(c => $"- {c.Name}");
            
            return $"""
                📂 **Categorias de Produtos**
                
                {string.Join("\n", categories)}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar categorias: {ex.Message}";
        }
    }
}
