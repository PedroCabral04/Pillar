namespace erp.DTOs.Kanban;

public record KanbanBoardDto(int Id, string Name);

public record KanbanColumnDto(int Id, string Title, int Position);

public record KanbanCardDto(int Id, string Title, string? Description, int Position, int ColumnId);

public record CreateCardRequest(int ColumnId, string Title, string? Description);

public record UpdateCardRequest(string Title, string? Description);

public record MoveCardRequest(int CardId, int ToColumnId, int ToPosition);

public record CreateColumnRequest(string Title);

public record RenameColumnRequest(string Title);

public record ReorderColumnRequest(int ColumnId, int NewPosition);

public record CreateBoardRequest(string Name);

public record RenameBoardRequest(string Name);
