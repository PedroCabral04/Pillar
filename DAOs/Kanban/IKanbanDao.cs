using erp.DTOs.Kanban;
using erp.Models.Kanban;

namespace erp.DAOs.Kanban;

public interface IKanbanDao
{
    // Boards
    Task<List<KanbanBoard>> GetBoardsByOwnerAsync(int userId);
    Task<KanbanBoard?> GetBoardByIdAsync(int id);
    Task<KanbanBoard> CreateBoardAsync(KanbanBoard board);
    Task UpdateBoardAsync(KanbanBoard board);
    Task DeleteBoardAsync(KanbanBoard board);
    Task<int> GetBoardCountByOwnerAsync(int userId);

    // Columns
    Task<List<KanbanColumn>> GetColumnsByBoardAsync(int boardId);
    Task<KanbanColumn?> GetColumnByIdAsync(int id);
    Task<KanbanColumn> CreateColumnAsync(KanbanColumn column);
    Task UpdateColumnAsync(KanbanColumn column);
    Task DeleteColumnAsync(KanbanColumn column);
    Task<int> GetMaxColumnPositionAsync(int boardId);
    Task<List<KanbanColumn>> GetColumnsAfterAsync(int boardId, int position);

    // Cards
    Task<KanbanCard?> GetCardByIdAsync(int id);
    Task<List<KanbanCard>> GetCardsByColumnAsync(int columnId, bool includeArchived = false);
    Task<KanbanCard> CreateCardAsync(KanbanCard card);
    Task UpdateCardAsync(KanbanCard card);
    Task DeleteCardAsync(KanbanCard card);
    Task<int> GetMaxCardPositionAsync(int columnId);
    Task<List<KanbanCard>> GetCardsAfterAsync(int columnId, int position);
    // Task<List<KanbanCard>> GetCardsInBoardAsync(int boardId); // Removed as per review

    // Card Labels (junction)
    Task AddCardLabelAsync(KanbanCardLabel cardLabel);
    Task AddCardLabelsAsync(IEnumerable<KanbanCardLabel> cardLabels);
    Task RemoveCardLabelAsync(KanbanCardLabel cardLabel);
    Task RemoveCardLabelsAsync(IEnumerable<KanbanCardLabel> cardLabels);
    Task<List<KanbanCardLabel>> GetCardLabelsAsync(int cardId);
    Task ClearCardLabelsAsync(int cardId);

    // Comments
    Task<KanbanComment?> GetCommentByIdAsync(int id);
    Task<List<KanbanComment>> GetCommentsByCardAsync(int cardId);
    Task<KanbanComment> CreateCommentAsync(KanbanComment comment);
    Task UpdateCommentAsync(KanbanComment comment);
    Task DeleteCommentAsync(KanbanComment comment);

    // History
    Task<List<KanbanCardHistory>> GetHistoryByCardAsync(int cardId);
    Task<KanbanCardHistory> CreateHistoryAsync(KanbanCardHistory history);

    // Labels
    Task<List<KanbanLabel>> GetLabelsByBoardAsync(int boardId);
    Task<KanbanLabel?> GetLabelByIdAsync(int id);
    Task<KanbanLabel> CreateLabelAsync(KanbanLabel label);
    Task UpdateLabelAsync(KanbanLabel label);
    Task DeleteLabelAsync(KanbanLabel label);

    // Stats
    Task<KanbanStatsDto> GetStatsAsync(int boardId);
}
