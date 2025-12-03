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

    [KernelFunction, Description("Lista todos os ativos cadastrados no sistema. Use página > 1 para ver mais.")]
    public async Task<string> ListAssets(
        [Description("Número máximo de ativos a retornar por página")] int maxResults = 10,
        [Description("Número da página (1 = primeira, 2 = próxima, etc)")] int page = 1)
    {
        try
        {
            var assets = await _assetService.GetAllAssetsAsync();

            if (!assets.Any())
            {
                return "📦 Não há ativos cadastrados no momento.";
            }

            var skip = (page - 1) * maxResults;
            var paged = assets.Skip(skip).Take(maxResults);
            
            if (!paged.Any())
            {
                return $"📦 Não há mais ativos. Total: {assets.Count} ativos.";
            }

            var assetList = paged.Select(a =>
                $"| `{a.AssetCode}` | {a.Name} | {GetStatusText(a.Status)} | {a.CurrentAssignedToUserName ?? "—"} |"
            );
            
            var shown = skip + paged.Count();
            var remaining = assets.Count - shown;
            
            var pageInfo = page > 1 ? $" (Página {page})" : "";
            var moreText = remaining > 0 
                ? $"\n\n*Exibindo {shown} de {assets.Count}. Peça \"listar ativos página {page + 1}\" para ver mais.*" 
                : "";

            return $"""
                📦 **Ativos Cadastrados**{pageInfo} ({assets.Count} total)
                
                | Código | Nome | Status | Responsável |
                |--------|------|--------|-------------|
                {string.Join("\n", assetList)}{moreText}
                """;
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
            var asset = await _assetService.GetAssetByCodeAsync(searchTerm);

            if (asset == null)
            {
                var allAssets = await _assetService.GetAllAssetsAsync();
                asset = allAssets.FirstOrDefault(a =>
                    a.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.AssetCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (asset == null)
            {
                return $"🔍 Ativo **'{searchTerm}'** não encontrado.";
            }

            var assignmentInfo = asset.CurrentAssignedToUserName != null
                ? $"{asset.CurrentAssignedToUserName} (desde {asset.CurrentAssignedDate:dd/MM/yyyy})"
                : "— (disponível)";

            return $"""
                📦 **Ativo Encontrado**
                
                | Campo | Valor |
                |-------|-------|
                | **Código** | `{asset.AssetCode}` |
                | **Nome** | {asset.Name} |
                | **Descrição** | {asset.Description ?? "—"} |
                | **Categoria** | {asset.CategoryName} |
                | **Status** | {GetStatusText(asset.Status)} |
                | **Condição** | {GetConditionText(asset.Condition)} |
                | **Local** | {asset.Location ?? "—"} |
                | **Responsável** | {assignmentInfo} |
                | **Valor** | {(asset.PurchaseValue.HasValue ? $"R$ {asset.PurchaseValue:N2}" : "—")} |
                """;
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
                return $"🔍 Ativo com ID **{assetId}** não encontrado.";
            }

            var assignmentInfo = asset.CurrentAssignedToUserName != null
                ? $"{asset.CurrentAssignedToUserName} (desde {asset.CurrentAssignedDate:dd/MM/yyyy})"
                : "— (disponível)";

            return $"""
                📦 **Detalhes do Ativo #{asset.Id}**
                
                | Campo | Valor |
                |-------|-------|
                | **Código** | `{asset.AssetCode}` |
                | **Nome** | {asset.Name} |
                | **Descrição** | {asset.Description ?? "—"} |
                | **Categoria** | {asset.CategoryName} |
                | **Status** | {GetStatusText(asset.Status)} |
                | **Condição** | {GetConditionText(asset.Condition)} |
                | **Local** | {asset.Location ?? "—"} |
                | **Nº Série** | {asset.SerialNumber ?? "—"} |
                | **Fabricante** | {asset.Manufacturer ?? "—"} |
                | **Modelo** | {asset.Model ?? "—"} |
                | **Responsável** | {assignmentInfo} |
                | **Valor** | {(asset.PurchaseValue.HasValue ? $"R$ {asset.PurchaseValue:N2}" : "—")} |
                | **Data Compra** | {(asset.PurchaseDate.HasValue ? asset.PurchaseDate.Value.ToString("dd/MM/yyyy") : "—")} |
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar detalhes: {ex.Message}";
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
                return $"👤 O usuário (ID: {userId}) não possui ativos atribuídos.";
            }

            var assetList = assignments.Take(10).Select(a =>
                $"| `{a.AssetCode}` | {a.AssetName} | {a.AssignedDate:dd/MM/yyyy} |"
            );
            
            var remaining = assignments.Count() - 10;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} ativos.*" : "";

            return $"""
                👤 **Ativos do Usuário** (ID: {userId})
                
                | Código | Nome | Desde |
                |--------|------|-------|
                {string.Join("\n", assetList)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar ativos: {ex.Message}";
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
                return "✅ Não há manutenções em atraso.";
            }

            var list = maintenances.Take(10).Select(m =>
                $"| `{m.AssetCode}` | {m.AssetName} | {m.Description} | {m.ScheduledDate:dd/MM} | R$ {m.Cost:N2} |"
            );
            
            var remaining = maintenances.Count - 10;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} manutenções.*" : "";

            return $"""
                ⚠️ **Manutenções em Atraso** ({maintenances.Count})
                
                | Código | Ativo | Descrição | Agendado | Custo |
                |--------|-------|-----------|----------|-------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar manutenções: {ex.Message}";
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
                return "📅 Não há manutenções agendadas.";
            }

            var list = maintenances.Take(10).Select(m =>
                $"| `{m.AssetCode}` | {m.AssetName} | {m.Description} | {m.ScheduledDate:dd/MM} | R$ {m.Cost:N2} |"
            );
            
            var remaining = maintenances.Count - 10;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} manutenções.*" : "";

            return $"""
                📅 **Manutenções Agendadas** ({maintenances.Count})
                
                | Código | Ativo | Descrição | Data | Custo |
                |--------|-------|-----------|------|-------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar manutenções: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém estatísticas gerais sobre os ativos da empresa")]
    public async Task<string> GetAssetStatistics()
    {
        try
        {
            var stats = await _assetService.GetAssetStatisticsAsync();

            var categoryBreakdown = stats.AssetsByCategory.Any()
                ? string.Join(", ", stats.AssetsByCategory.Take(5).Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "—";

            return $"""
                📊 **Estatísticas de Ativos**
                
                | Métrica | Valor |
                |---------|-------|
                | **Total** | {stats.TotalAssets} |
                | **Disponíveis** | {stats.AvailableAssets} |
                | **Em Uso** | {stats.AssignedAssets} |
                | **Manutenção** | {stats.InMaintenanceAssets} |
                | **Desativados** | {stats.RetiredAssets} |
                
                ---
                💰 **Valor Total:** R$ {stats.TotalAssetValue:N2}
                
                🔧 **Manutenções:** {stats.ScheduledMaintenances} agendadas, {stats.OverdueMaintenances} atrasadas
                
                📁 **Categorias:** {categoryBreakdown}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao obter estatísticas: {ex.Message}";
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
                return "❌ Status inválido. Use: `Available`, `InUse`, `Maintenance` ou `Retired`.";
            }

            var assets = await _assetService.GetAssetsByStatusAsync(assetStatus);

            if (!assets.Any())
            {
                return $"📦 Nenhum ativo com status **{GetStatusText(assetStatus)}**.";
            }

            var list = assets.Take(10).Select(a =>
                $"| `{a.AssetCode}` | {a.Name} | {a.CategoryName} |"
            );
            
            var remaining = assets.Count - 10;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} ativos.*" : "";

            return $"""
                📦 **Ativos {GetStatusText(assetStatus)}** ({assets.Count})
                
                | Código | Nome | Categoria |
                |--------|------|-----------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar ativos: {ex.Message}";
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

            var list = categories.Take(15).Select(c =>
                $"| {c.Name} | {c.Description ?? "—"} |"
            );

            return $"""
                📁 **Categorias de Ativos** ({categories.Count})
                
                | Categoria | Descrição |
                |-----------|-----------|
                {string.Join("\n", list)}
                """;
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
