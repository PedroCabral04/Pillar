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

    public ProductsPlugin(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [KernelFunction, Description("Lista todos os produtos cadastrados no sistema. Use página > 1 para ver mais produtos.")]
    public async Task<string> ListProducts(
        [Description("Número máximo de produtos a retornar por página")] int maxResults = 10,
        [Description("Número da página (1 = primeira página, 2 = próxima, etc)")] int page = 1)
    {
        try
        {
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

            return $"📦 **Produtos Cadastrados**{pageInfo} ({result.TotalCount} total)\n\n{string.Join("\n", productList)}{moreText}";
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

    [KernelFunction, Description("Cadastra um novo produto no sistema")]
    public async Task<string> CreateProduct(
        [Description("Nome do produto")] string name,
        [Description("SKU/Código do produto")] string sku,
        [Description("Preço do produto")] decimal price,
        [Description("Descrição do produto")] string description = "",
        [Description("Categoria do produto")] string category = "Geral",
        [Description("Quantidade inicial em estoque")] int initialQuantity = 0)
    {
        try
        {
            var productDto = new CreateProductDto
            {
                Name = name,
                Sku = sku,
                Description = description,
                SalePrice = price,
                CategoryId = 1 // Default category - TODO: Allow specifying category
            };

            var createdProduct = await _inventoryService.CreateProductAsync(productDto, 1); // TODO: Obter userId do contexto

            return $"""
                ✅ **Produto Cadastrado com Sucesso!**
                
                - **Nome:** {createdProduct.Name}
                - **SKU:** `{createdProduct.Sku}`
                - **Preço:** R$ {createdProduct.SalePrice:N2}
                - **Estoque:** {createdProduct.CurrentStock} unidades
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
}
