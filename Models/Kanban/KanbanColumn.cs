using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace erp.Models.Kanban;

public class KanbanColumn
{
    public int Id { get; set; }

    public int BoardId { get; set; }
    public KanbanBoard Board { get; set; } = null!;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // position within board (0..N)
    public int Position { get; set; }

    public List<KanbanCard> Cards { get; set; } = new();
}
