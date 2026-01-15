using erp.DTOs.Kanban;
using erp.Models.Kanban;

namespace erp.Services.Kanban;

public interface IKanbanService
{
    // Boards
    Task<List<KanbanBoardDto>> GetMyBoardsAsync(int userId);
    Task<KanbanBoardDto> CreateBoardAsync(CreateBoardRequest req, int userId, int tenantId);
    Task<KanbanBoardDto?> GetBoardAsync(int id, int userId);
    Task RenameBoardAsync(int id, string name, int userId);
    Task DeleteBoardAsync(int id, int userId);
    Task<KanbanBoardDto> GetOrCreateMyBoardAsync(int userId, int tenantId);

    // Columns
    Task<List<ColumnWithCardsDto>> GetColumnsAsync(int? boardId, int userId);
    Task<KanbanColumnDto> CreateColumnAsync(CreateColumnRequest req, int userId, int tenantId);
    Task RenameColumnAsync(int id, string title, int userId);
    Task DeleteColumnAsync(int id, int userId);
    Task ReorderColumnAsync(int columnId, int newPosition, int userId);

    // Cards
    Task<KanbanCardDto?> GetCardAsync(int id, int userId);
    Task<KanbanCardDto> CreateCardAsync(CreateCardRequest req, int userId, int tenantId);
    Task UpdateCardAsync(int id, UpdateCardRequest req, int userId);
    Task DeleteCardAsync(int id, int userId);
    Task ArchiveCardAsync(int id, bool isArchived, int userId);
    Task MoveCardAsync(MoveCardRequest req, int userId);

    // Comments
    Task<List<KanbanCommentDto>> GetCommentsAsync(int cardId, int userId);
    Task<KanbanCommentDto> CreateCommentAsync(int cardId, CreateCommentRequest req, int userId);
    Task UpdateCommentAsync(int cardId, int commentId, UpdateCommentRequest req, int userId);
    Task DeleteCommentAsync(int cardId, int commentId, int userId);

    // History
    Task<List<KanbanCardHistoryDto>> GetHistoryAsync(int cardId, int userId);

    // Labels
    Task<List<KanbanLabelDto>> GetLabelsAsync(int? boardId, int userId);
    Task<KanbanLabelDto> CreateLabelAsync(CreateLabelRequest req, int userId);
    Task UpdateLabelAsync(int id, UpdateLabelRequest req, int userId);
    Task DeleteLabelAsync(int id, int userId);

    // Stats
    Task<KanbanStatsDto> GetStatsAsync(int? boardId, int userId);

    // Users
    Task<List<AssignableUserDto>> GetAssignableUsersAsync();
}

public record ColumnWithCardsDto(
    int Id,
    string Title,
    int Position,
    List<KanbanCardDto> Cards
);

public record AssignableUserDto(int Id, string FullName, string? Photo);
