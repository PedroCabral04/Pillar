using System;
using System.ComponentModel.DataAnnotations;

namespace erp.Models.Kanban;

public class KanbanCard
{
    public int Id { get; set; }

    public int ColumnId { get; set; }
    public KanbanColumn Column { get; set; } = null!;

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    // position within column (0..N)
    public int Position { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
}
