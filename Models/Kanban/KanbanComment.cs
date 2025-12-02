using System.ComponentModel.DataAnnotations;
using erp.Models.Identity;

namespace erp.Models.Kanban;

/// <summary>
/// Comentários em cards do Kanban
/// </summary>
public class KanbanComment
{
    public int Id { get; set; }

    public int CardId { get; set; }
    public KanbanCard Card { get; set; } = null!;

    public int AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsEdited { get; set; } = false;
}
