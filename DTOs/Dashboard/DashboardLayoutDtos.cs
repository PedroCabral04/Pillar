namespace erp.DTOs.Dashboard;

/// <summary>
/// Representa a configuração de layout do dashboard de um usuário.
/// </summary>
public class DashboardLayout
{
    /// <summary>Identificador do usuário dono deste layout.</summary>
    public required string UserId { get; set; }

    /// <summary>Lista de widgets configurados no layout.</summary>
    public List<WidgetConfiguration> Widgets { get; set; } = new();

    /// <summary>Tipo de layout (ex.: "grid", "list", "compact").</summary>
    public string LayoutType { get; set; } = "grid"; // grid, list, compact

    /// <summary>Número de colunas do layout em modo grid.</summary>
    public int Columns { get; set; } = 3;

    /// <summary>Data/hora da última modificação no layout (UTC).</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configuração individual de um widget no layout do dashboard.
/// </summary>
public class WidgetConfiguration
{
    /// <summary>Identificador único do widget na configuração do usuário.</summary>
    public required string WidgetId { get; set; }

    /// <summary>Chave do provedor do widget.</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Chave do widget fornecida pelo provedor.</summary>
    public required string WidgetKey { get; set; }

    /// <summary>Ordem do widget para renderização.</summary>
    public int Order { get; set; }

    /// <summary>Posição em linha (grid).</summary>
    public int Row { get; set; }

    /// <summary>Posição em coluna (grid).</summary>
    public int Column { get; set; }

    /// <summary>Largura do widget em unidades de grid.</summary>
    public int Width { get; set; } = 1; // Grid units

    /// <summary>Altura do widget em unidades de grid.</summary>
    public int Height { get; set; } = 1; // Grid units

    /// <summary>Indica se o widget está visível.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Indica se o widget está colapsado.</summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>Configurações personalizadas do widget (serializáveis).</summary>
    public Dictionary<string, object>? CustomSettings { get; set; }
}

/// <summary>
/// Item de catálogo descrevendo um widget disponível no sistema.
/// </summary>
public class WidgetCatalogItem
{
    /// <summary>Chave do provedor do widget.</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Chave do widget.</summary>
    public required string WidgetKey { get; set; }

    /// <summary>Título amigável do widget.</summary>
    public required string Title { get; set; }

    /// <summary>Descrição do que o widget faz.</summary>
    public required string Description { get; set; }

    /// <summary>Ícone opcional (classe ou caminho) para o widget.</summary>
    public string? Icon { get; set; }

    /// <summary>Categoria do widget (ex.: "Financeiro", "Vendas").</summary>
    public string? Category { get; set; }

    /// <summary>Indica se o widget precisa de configuração antes de uso.</summary>
    public bool RequiresConfiguration { get; set; }

    /// <summary>Roles necessárias para visualizar/usar o widget (opcional).</summary>
    public string[]? RequiredRoles { get; set; }
}

/// <summary>
/// DTO usado para salvar o layout do usuário.
/// </summary>
public class SaveLayoutRequest
{
    /// <summary>Lista de widgets para persistir no layout.</summary>
    public required List<WidgetConfiguration> Widgets { get; set; }

    /// <summary>Tipo de layout (ex.: "grid").</summary>
    public string LayoutType { get; set; } = "grid";

    /// <summary>Número de colunas quando em modo grid.</summary>
    public int Columns { get; set; } = 3;
}

/// <summary>
/// DTO usado para adicionar um widget ao layout do usuário.
/// </summary>
public class AddWidgetRequest
{
    /// <summary>Chave do provedor do widget a ser adicionado.</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Chave do widget a ser adicionado.</summary>
    public required string WidgetKey { get; set; }

    /// <summary>Posição desejada em linha (opcional).</summary>
    public int? Row { get; set; }

    /// <summary>Posição desejada em coluna (opcional).</summary>
    public int? Column { get; set; }
}

/// <summary>
/// DTO para atualizar propriedades de um widget existente no layout.
/// </summary>
public class UpdateWidgetRequest
{
    /// <summary>Nova ordem do widget (opcional).</summary>
    public int? Order { get; set; }

    /// <summary>Nova posição em linha (opcional).</summary>
    public int? Row { get; set; }

    /// <summary>Nova posição em coluna (opcional).</summary>
    public int? Column { get; set; }

    /// <summary>Nova largura em unidades de grid (opcional).</summary>
    public int? Width { get; set; }

    /// <summary>Nova altura em unidades de grid (opcional).</summary>
    public int? Height { get; set; }

    /// <summary>Define visibilidade do widget (opcional).</summary>
    public bool? IsVisible { get; set; }

    /// <summary>Define se o widget ficará colapsado (opcional).</summary>
    public bool? IsCollapsed { get; set; }

    /// <summary>Configurações customizadas (opcional).</summary>
    public Dictionary<string, object>? CustomSettings { get; set; }
}
