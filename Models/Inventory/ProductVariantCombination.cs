namespace erp.Models.Inventory;

/// <summary>
/// Tabela de junção N:M entre ProductVariant e ProductVariantOptionValue.
/// Define quais valores de opção compõem cada variant.
/// </summary>
public class ProductVariantCombination
{
    public int VariantId { get; set; }
    public virtual ProductVariant Variant { get; set; } = null!;
    
    public int OptionValueId { get; set; }
    public virtual ProductVariantOptionValue OptionValue { get; set; } = null!;
}
