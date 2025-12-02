using System.ComponentModel.DataAnnotations;

namespace erp.Models.Kanban;

/// <summary>
/// Etiquetas compartilhadas no quadro Kanban para categorização de cards
/// </summary>
public class KanbanLabel
{
    public int Id { get; set; }

    public int BoardId { get; set; }
    public KanbanBoard Board { get; set; } = null!;

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cor da etiqueta em formato hex (ex: #FF5733)
    /// </summary>
    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
