using erp.Models.Kanban;

namespace erp.DTOs.Kanban;

public record KanbanBoardDto(int Id, string Name, DateTime CreatedAt);

public record KanbanColumnDto(int Id, string Title, int Position);

public record KanbanCardDto(
    int Id, 
    string Title, 
    string? Description, 
    int Position, 
    int ColumnId,
    DateTime CreatedAt,
    DateTime? DueDate,
    KanbanPriority Priority,
    string? Color,
    int? AssignedUserId,
    string? AssignedUserName,
    string? AssignedUserPhoto,
    DateTime? CompletedAt,
    bool IsArchived,
    List<KanbanLabelDto> Labels,
    int CommentCount
);

public record KanbanLabelDto(int Id, string Name, string Color);

public record KanbanCommentDto(
    int Id,
    int CardId,
    int AuthorId,
    string AuthorName,
    string? AuthorPhoto,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsEdited
);

public record KanbanCardHistoryDto(
    int Id,
    int CardId,
    int UserId,
    string UserName,
    string Action,
    string Description,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt
);

// ===== Requests =====

public record CreateCardRequest(
    int ColumnId, 
    string Title, 
    string? Description,
    DateTime? DueDate = null,
    KanbanPriority Priority = KanbanPriority.None,
    int? AssignedUserId = null,
    string? Color = null,
    List<int>? LabelIds = null
);

public record UpdateCardRequest(
    string Title, 
    string? Description,
    DateTime? DueDate = null,
    KanbanPriority Priority = KanbanPriority.None,
    int? AssignedUserId = null,
    string? Color = null,
    List<int>? LabelIds = null,
    DateTime? CompletedAt = null
);

public record MoveCardRequest(int CardId, int ToColumnId, int ToPosition);

public record CreateColumnRequest(string Title);

public record RenameColumnRequest(string Title);

public record ReorderColumnRequest(int ColumnId, int NewPosition);

public record CreateBoardRequest(string Name);

public record RenameBoardRequest(string Name);

// Labels
public record CreateLabelRequest(string Name, string Color);

public record UpdateLabelRequest(string Name, string Color);

// Comments
public record CreateCommentRequest(string Content);

public record UpdateCommentRequest(string Content);

// Arquivar
public record ArchiveCardRequest(bool IsArchived);

// Estatísticas
public record KanbanStatsDto(
    int TotalCards,
    int CompletedCards,
    int OverdueCards,
    int HighPriorityCards,
    int UnassignedCards
);
