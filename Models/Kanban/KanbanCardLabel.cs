namespace erp.Models.Kanban;

/// <summary>
/// Relacionamento muitos-para-muitos entre cards e labels
/// </summary>
public class KanbanCardLabel
{
    public int CardId { get; set; }
    public KanbanCard Card { get; set; } = null!;

    public int LabelId { get; set; }
    public KanbanLabel Label { get; set; } = null!;
}
