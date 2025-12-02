using System.ComponentModel.DataAnnotations;
using erp.Models.Identity;

namespace erp.Models.Kanban;

/// <summary>
/// Tipos de ações no histórico do card
/// </summary>
public enum KanbanHistoryAction
{
    Created,
    Updated,
    Moved,
    AssignedUser,
    UnassignedUser,
    LabelAdded,
    LabelRemoved,
    DueDateSet,
    DueDateRemoved,
    PriorityChanged,
    CommentAdded,
    CommentEdited,
    CommentDeleted,
    Archived,
    Restored
}

/// <summary>
/// Histórico de atividades em cards do Kanban
/// </summary>
public class KanbanCardHistory
{
    public int Id { get; set; }

    public int CardId { get; set; }
    public KanbanCard Card { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public KanbanHistoryAction Action { get; set; }

    /// <summary>
    /// Descrição legível da ação (ex: "moveu de 'A Fazer' para 'Fazendo'")
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor anterior (JSON ou texto simples)
    /// </summary>
    [MaxLength(1000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// Novo valor (JSON ou texto simples)
    /// </summary>
    [MaxLength(1000)]
    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
