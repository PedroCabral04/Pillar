namespace erp.Models.ServiceOrders;

/// <summary>
/// Status de uma Ordem de Serviço
/// </summary>
public enum ServiceOrderStatus
{
    /// <summary>Aberto - Ordem recém-criada</summary>
    Open = 0,

    /// <summary>Em Andamento - Técnico trabalhando no serviço</summary>
    InProgress = 1,

    /// <summary>Aguardando Cliente - Aguardando aprovação ou resposta do cliente</summary>
    WaitingCustomer = 2,

    /// <summary>Aguardando Peças - Aguardando chegada de peças</summary>
    WaitingParts = 3,

    /// <summary>Concluído - Serviço finalizado, pronto para entrega</summary>
    Completed = 4,

    /// <summary>Entregue - Entregue ao cliente</summary>
    Delivered = 5,

    /// <summary>Cancelado - Ordem cancelada</summary>
    Cancelled = 6
}

/// <summary>
/// Tipos de garantia para serviços
/// </summary>
public enum WarrantyType
{
    /// <summary>Sem garantia</summary>
    None = 0,

    /// <summary>30 dias de garantia</summary>
    Days30 = 1,

    /// <summary>90 dias de garantia</summary>
    Days90 = 2,

    /// <summary>180 dias de garantia</summary>
    Days180 = 3,

    /// <summary>365 dias de garantia</summary>
    Days365 = 4
}

/// <summary>
/// Tipos de aparelhos para serviço
/// </summary>
public enum DeviceType
{
    /// <summary>Smartphone</summary>
    Smartphone = 0,

    /// <summary>Tablet</summary>
    Tablet = 1,

    /// <summary>Notebook</summary>
    Notebook = 2,

    /// <summary>Desktop</summary>
    Desktop = 3,

    /// <summary>Smartwatch</summary>
    Smartwatch = 4,

    /// <summary>Video Game</summary>
    VideoGame = 5,

    /// <summary>TV</summary>
    TV = 6,

    /// <summary>Outro</summary>
    Other = 99
}
