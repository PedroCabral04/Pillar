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

    [KernelFunction, Description("Lista todos os produtos cadastrados no sistema")]
    public async Task<string> ListProducts()
    {
        try
        {
            var result = await _inventoryService.SearchProductsAsync(new ProductSearchDto 
            { 
                PageSize = 20 
            });
            
            if (!result.Products.Any())
            {
                return "Não há produtos cadastrados no momento.";
            }

            var productList = result.Products.Select(p => 
                $"- **{p.Name}** (SKU: {p.Sku})\n  R$ {p.SalePrice:F2} - Estoque: {p.CurrentStock} unidades"
            );

            return $"Produtos cadastrados:\n\n{string.Join("\n\n", productList)}";
        }
        catch (Exception ex)
        {
            return $"Erro ao listar produtos: {ex.Message}";
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
                return $"Produto '{searchTerm}' não encontrado. Deseja cadastrá-lo?";
            }

            return $"Produto encontrado:\n" +
                   $"Nome: {product.Name}\n" +
                   $"SKU: {product.Sku}\n" +
                   $"Descrição: {product.Description}\n" +
                   $"Preço: R$ {product.SalePrice:F2}\n" +
                   $"Quantidade em estoque: {product.CurrentStock} unidades\n" +
                   $"Categoria: {product.CategoryName}";
        }
        catch (Exception ex)
        {
            return $"Erro ao buscar produto: {ex.Message}";
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

            return $"✅ Produto cadastrado com sucesso!\n" +
                   $"Nome: {createdProduct.Name}\n" +
                   $"SKU: {createdProduct.Sku}\n" +
                   $"Preço: R$ {createdProduct.SalePrice:F2}\n" +
                   $"Estoque inicial: {createdProduct.CurrentStock} unidades";
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
                return $"Produto '{productIdentifier}' não encontrado.";
            }

            var status = product.CurrentStock switch
            {
                0 => "⚠️ SEM ESTOQUE",
                < 10 => "⚠️ ESTOQUE BAIXO",
                _ => "✅ ESTOQUE OK"
            };

            return $"{status}\n" +
                   $"Produto: {product.Name}\n" +
                   $"Quantidade disponível: {product.CurrentStock} unidades";
        }
        catch (Exception ex)
        {
            return $"Erro ao verificar estoque: {ex.Message}";
        }
    }
}
