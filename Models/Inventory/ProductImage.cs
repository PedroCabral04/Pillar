namespace erp.Models.Inventory;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Position { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
