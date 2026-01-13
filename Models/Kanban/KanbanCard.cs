using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using erp.Models.Identity;
using erp.Models;

namespace erp.Models.Kanban;

public class KanbanCard : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }

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

    // ===== Novos campos =====

    /// <summary>
    /// Usuário responsável pelo card
    /// </summary>
    public int? AssignedUserId { get; set; }
    public ApplicationUser? AssignedUser { get; set; }

    /// <summary>
    /// Prioridade do card
    /// </summary>
    public KanbanPriority Priority { get; set; } = KanbanPriority.None;

    /// <summary>
    /// Cor de destaque do card (hex, ex: #FF5733)
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Card arquivado (soft delete)
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Data de conclusão
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ===== Navegação =====

    public List<KanbanCardLabel> CardLabels { get; set; } = new();
    public List<KanbanComment> Comments { get; set; } = new();
    public List<KanbanCardHistory> History { get; set; } = new();
}
