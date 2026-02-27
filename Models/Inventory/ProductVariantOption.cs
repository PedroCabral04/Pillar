namespace erp.Models.Inventory;

/// <summary>
/// Define um eixo de variação para um produto (ex: Cor, Tamanho, Material)
/// </summary>
public class ProductVariantOption
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    
    /// <summary>
    /// Nome da opção (ex: "Cor", "Tamanho")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Ordem de exibição
    /// </summary>
    public int Position { get; set; } = 0;
    
    /// <summary>
    /// Valores possíveis desta opção
    /// </summary>
    public virtual ICollection<ProductVariantOptionValue> Values { get; set; } = new List<ProductVariantOptionValue>();
}
