using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Assets;
using erp.Models;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para gerenciar ativos (patrimônio) através do chatbot
/// </summary>
public class AssetsPlugin
{
    private readonly IAssetService _assetService;

    public AssetsPlugin(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [KernelFunction, Description("Lista todos os ativos cadastrados no sistema")]
    public async Task<string> ListAssets(
        [Description("Número máximo de ativos a retornar")] int maxResults = 20)
    {
        try
        {
            var assets = await _assetService.GetAllAssetsAsync();

            if (!assets.Any())
            {
                return "Não há ativos cadastrados no momento.";
            }

            var assetList = assets.Take(maxResults).Select(a =>
                $"- **{a.Name}** (Código: {a.AssetCode}) - Status: {GetStatusText(a.Status)} - {(a.CurrentAssignedToUserName != null ? $"Atribuído a: {a.CurrentAssignedToUserName}" : "Disponível")}"
            );

            return $"📦 **Ativos cadastrados ({assets.Count} total):**\n{string.Join("\n", assetList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar ativos: {ex.Message}";
        }
    }

    [KernelFunction, Description("Busca um ativo específico pelo código ou nome")]
    public async Task<string> SearchAsset(
        [Description("Código ou nome do ativo a ser buscado")] string searchTerm)
    {
        try
        {
            // Primeiro tenta buscar por código exato
            var asset = await _assetService.GetAssetByCodeAsync(searchTerm);

            if (asset == null)
            {
                // Se não encontrar por código, busca na lista geral pelo nome
                var allAssets = await _assetService.GetAllAssetsAsync();
                asset = allAssets.FirstOrDefault(a =>
                    a.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.AssetCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (asset == null)
            {
                return $"🔍 Ativo '{searchTerm}' não encontrado. Verifique o código ou nome do ativo.";
            }

            var assignmentInfo = asset.CurrentAssignedToUserName != null
                ? $"Atribuído a: {asset.CurrentAssignedToUserName} desde {asset.CurrentAssignedDate:dd/MM/yyyy}"
                : "Não atribuído (disponível)";

            return $"📦 **Ativo encontrado:**\n" +
                   $"**Código:** {asset.AssetCode}\n" +
                   $"**Nome:** {asset.Name}\n" +
                   $"**Descrição:** {asset.Description ?? "Sem descrição"}\n" +
                   $"**Categoria:** {asset.CategoryName}\n" +
                   $"**Status:** {GetStatusText(asset.Status)}\n" +
                   $"**Condição:** {GetConditionText(asset.Condition)}\n" +
                   $"**Localização:** {asset.Location ?? "Não informada"}\n" +
                   $"**{assignmentInfo}**\n" +
                   $"**Valor de compra:** {(asset.PurchaseValue.HasValue ? $"R$ {asset.PurchaseValue:F2}" : "Não informado")}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar ativo: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém detalhes completos de um ativo pelo ID")]
    public async Task<string> GetAssetDetails(
        [Description("ID do ativo")] int assetId)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId);

            if (asset == null)
            {
                return $"🔍 Ativo com ID {assetId} não encontrado.";
            }

            var assignmentInfo = asset.CurrentAssignedToUserName != null
                ? $"Atribuído a: {asset.CurrentAssignedToUserName} desde {asset.CurrentAssignedDate:dd/MM/yyyy}"
                : "Não atribuído (disponível)";

            return $"📦 **Detalhes do Ativo:**\n" +
                   $"**ID:** {asset.Id}\n" +
                   $"**Código:** {asset.AssetCode}\n" +
                   $"**Nome:** {asset.Name}\n" +
                   $"**Descrição:** {asset.Description ?? "Sem descrição"}\n" +
                   $"**Categoria:** {asset.CategoryName}\n" +
                   $"**Status:** {GetStatusText(asset.Status)}\n" +
                   $"**Condição:** {GetConditionText(asset.Condition)}\n" +
                   $"**Localização:** {asset.Location ?? "Não informada"}\n" +
                   $"**Número de série:** {asset.SerialNumber ?? "Não informado"}\n" +
                   $"**Fabricante:** {asset.Manufacturer ?? "Não informado"}\n" +
                   $"**Modelo:** {asset.Model ?? "Não informado"}\n" +
                   $"**{assignmentInfo}**\n" +
                   $"**Valor de compra:** {(asset.PurchaseValue.HasValue ? $"R$ {asset.PurchaseValue:F2}" : "Não informado")}\n" +
                   $"**Data de compra:** {(asset.PurchaseDate.HasValue ? asset.PurchaseDate.Value.ToString("dd/MM/yyyy") : "Não informada")}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar detalhes do ativo: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista os ativos atribuídos a um usuário específico")]
    public async Task<string> GetAssetsAssignedToUser(
        [Description("ID do usuário para buscar os ativos atribuídos")] int userId)
    {
        try
        {
            var assignments = await _assetService.GetAssignmentsForUserAsync(userId, includeReturned: false);

            if (!assignments.Any())
            {
                return $"👤 O usuário com ID {userId} não possui ativos atribuídos no momento.";
            }

            var assetList = assignments.Select(a =>
                $"- **{a.AssetName}** (Código: {a.AssetCode}) - Desde: {a.AssignedDate:dd/MM/yyyy}"
            );

            return $"👤 **Ativos atribuídos ao usuário (ID: {userId}):**\n{string.Join("\n", assetList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar ativos do usuário: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista todas as manutenções de ativos em atraso")]
    public async Task<string> GetOverdueMaintenances()
    {
        try
        {
            var maintenances = await _assetService.GetOverdueMaintenancesAsync();

            if (!maintenances.Any())
            {
                return "✅ Não há manutenções em atraso no momento.";
            }

            var maintenanceList = maintenances.Select(m =>
                $"- **{m.AssetName}** ({m.AssetCode}) - {m.Description} - Agendada para: {m.ScheduledDate:dd/MM/yyyy} - Custo: R$ {m.Cost:F2}"
            );

            return $"⚠️ **Manutenções em atraso ({maintenances.Count}):**\n{string.Join("\n", maintenanceList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar manutenções em atraso: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista as manutenções de ativos agendadas")]
    public async Task<string> GetScheduledMaintenances()
    {
        try
        {
            var maintenances = await _assetService.GetScheduledMaintenancesAsync();

            if (!maintenances.Any())
            {
                return "📅 Não há manutenções agendadas no momento.";
            }

            var maintenanceList = maintenances.Select(m =>
                $"- **{m.AssetName}** ({m.AssetCode}) - {m.Description} - Data: {m.ScheduledDate:dd/MM/yyyy} - Custo: R$ {m.Cost:F2}"
            );

            return $"📅 **Manutenções agendadas ({maintenances.Count}):**\n{string.Join("\n", maintenanceList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar manutenções agendadas: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém estatísticas gerais sobre os ativos da empresa")]
    public async Task<string> GetAssetStatistics()
    {
        try
        {
            var stats = await _assetService.GetAssetStatisticsAsync();

            var categoryBreakdown = stats.AssetsByCategory.Any()
                ? string.Join(", ", stats.AssetsByCategory.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "Nenhum dado";

            return $"📊 **Estatísticas de Ativos:**\n\n" +
                   $"**Total de ativos:** {stats.TotalAssets}\n" +
                   $"**Disponíveis:** {stats.AvailableAssets}\n" +
                   $"**Em uso:** {stats.AssignedAssets}\n" +
                   $"**Em manutenção:** {stats.InMaintenanceAssets}\n" +
                   $"**Desativados:** {stats.RetiredAssets}\n\n" +
                   $"**Valor total do patrimônio:** R$ {stats.TotalAssetValue:N2}\n\n" +
                   $"**Manutenções agendadas:** {stats.ScheduledMaintenances}\n" +
                   $"**Manutenções em atraso:** {stats.OverdueMaintenances}\n\n" +
                   $"**Por categoria:** {categoryBreakdown}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao obter estatísticas de ativos: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista ativos por status específico (Disponível, Em Uso, Manutenção, Desativado)")]
    public async Task<string> GetAssetsByStatus(
        [Description("Status do ativo: Available (Disponível), InUse (Em Uso), Maintenance (Manutenção), Retired (Desativado)")] string status)
    {
        try
        {
            if (!Enum.TryParse<AssetStatus>(status, ignoreCase: true, out var assetStatus))
            {
                return "❌ Status inválido. Use: Available, InUse, Maintenance ou Retired.";
            }

            var assets = await _assetService.GetAssetsByStatusAsync(assetStatus);

            if (!assets.Any())
            {
                return $"📦 Não há ativos com status '{GetStatusText(assetStatus)}'.";
            }

            var assetList = assets.Select(a =>
                $"- **{a.Name}** (Código: {a.AssetCode}) - {a.CategoryName}"
            );

            return $"📦 **Ativos com status '{GetStatusText(assetStatus)}' ({assets.Count}):**\n{string.Join("\n", assetList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar ativos por status: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista todas as categorias de ativos disponíveis")]
    public async Task<string> ListAssetCategories()
    {
        try
        {
            var categories = await _assetService.GetAllCategoriesAsync();

            if (!categories.Any())
            {
                return "📁 Não há categorias de ativos cadastradas.";
            }

            var categoryList = categories.Select(c =>
                $"- **{c.Name}** - {c.Description ?? "Sem descrição"}"
            );

            return $"📁 **Categorias de ativos ({categories.Count}):**\n{string.Join("\n", categoryList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar categorias: {ex.Message}";
        }
    }

    private static string GetStatusText(AssetStatus status) => status switch
    {
        AssetStatus.Available => "✅ Disponível",
        AssetStatus.InUse => "👤 Em Uso",
        AssetStatus.Maintenance => "🔧 Manutenção",
        AssetStatus.Retired => "🚫 Desativado",
        _ => status.ToString()
    };

    private static string GetConditionText(AssetCondition condition) => condition switch
    {
        AssetCondition.Excellent => "🆕 Excelente",
        AssetCondition.Good => "👍 Bom",
        AssetCondition.Fair => "👌 Regular",
        AssetCondition.Poor => "⚠️ Ruim",
        AssetCondition.Damaged => "❌ Danificado",
        _ => condition.ToString()
    };
}
