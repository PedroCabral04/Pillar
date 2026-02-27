namespace erp.Models.Inventory;

/// <summary>
/// Valor possível de uma opção de variação (ex: "Vermelho", "G", "Algodão")
/// </summary>
public class ProductVariantOptionValue
{
    public int Id { get; set; }
    public int OptionId { get; set; }
    public virtual ProductVariantOption Option { get; set; } = null!;
    
    /// <summary>
    /// Valor da opção (ex: "Vermelho", "G")
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Ordem de exibição
    /// </summary>
    public int Position { get; set; } = 0;
    
    /// <summary>
    /// Combinações de variant que usam este valor
    /// </summary>
    public virtual ICollection<ProductVariantCombination> VariantCombinations { get; set; } = new List<ProductVariantCombination>();
}
