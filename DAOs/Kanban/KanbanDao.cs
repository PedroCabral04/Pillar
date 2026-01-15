using erp.Data;
using erp.DTOs.Kanban;
using erp.Models.Kanban;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Kanban;

public class KanbanDao : IKanbanDao
{
    private readonly ApplicationDbContext _context;

    public KanbanDao(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== Boards =====

    public async Task<List<KanbanBoard>> GetBoardsByOwnerAsync(int userId)
    {
        return await _context.KanbanBoards
            .AsNoTracking()
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<KanbanBoard?> GetBoardByIdAsync(int id)
    {
        return await _context.KanbanBoards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<KanbanBoard> CreateBoardAsync(KanbanBoard board)
    {
        _context.KanbanBoards.Add(board);
        await _context.SaveChangesAsync();
        return board;
    }

    public async Task UpdateBoardAsync(KanbanBoard board)
    {
        _context.KanbanBoards.Update(board);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBoardAsync(KanbanBoard board)
    {
        _context.KanbanBoards.Remove(board);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetBoardCountByOwnerAsync(int userId)
    {
        return await _context.KanbanBoards
            .Where(b => b.OwnerId == userId)
            .CountAsync();
    }

    // ===== Columns =====

    public async Task<List<KanbanColumn>> GetColumnsByBoardAsync(int boardId)
    {
        return await _context.KanbanColumns
            .AsNoTracking()
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Position)
            .ToListAsync();
    }

    public async Task<KanbanColumn?> GetColumnByIdAsync(int id)
    {
        return await _context.KanbanColumns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<KanbanColumn> CreateColumnAsync(KanbanColumn column)
    {
        _context.KanbanColumns.Add(column);
        await _context.SaveChangesAsync();
        return column;
    }

    public async Task UpdateColumnAsync(KanbanColumn column)
    {
        _context.KanbanColumns.Update(column);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteColumnAsync(KanbanColumn column)
    {
        _context.KanbanColumns.Remove(column);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetMaxColumnPositionAsync(int boardId)
    {
        return await _context.KanbanColumns
            .Where(c => c.BoardId == boardId)
            .MaxAsync(c => (int?)c.Position) ?? -1;
    }

    public async Task<List<KanbanColumn>> GetColumnsAfterAsync(int boardId, int position)
    {
        return await _context.KanbanColumns
            .Where(c => c.BoardId == boardId && c.Position > position)
            .ToListAsync();
    }

    // ===== Cards =====

    public async Task<KanbanCard?> GetCardByIdAsync(int id)
    {
        return await _context.KanbanCards
            .Include(c => c.Column).ThenInclude(c => c.Board)
            .Include(c => c.AssignedUser)
            .Include(c => c.CardLabels).ThenInclude(cl => cl.Label)
            .Include(c => c.Comments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<KanbanCard>> GetCardsByColumnAsync(int columnId, bool includeArchived = false)
    {
        var query = _context.KanbanCards
            .Include(c => c.AssignedUser)
            .Include(c => c.CardLabels).ThenInclude(cl => cl.Label)
            .Where(c => c.ColumnId == columnId);

        if (!includeArchived)
            query = query.Where(c => !c.IsArchived);

        return await query
            .OrderBy(c => c.Position)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<KanbanCard> CreateCardAsync(KanbanCard card)
    {
        _context.KanbanCards.Add(card);
        await _context.SaveChangesAsync();
        return card;
    }

    public async Task UpdateCardAsync(KanbanCard card)
    {
        _context.KanbanCards.Update(card);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCardAsync(KanbanCard card)
    {
        _context.KanbanCards.Remove(card);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetMaxCardPositionAsync(int columnId)
    {
        return await _context.KanbanCards
            .Where(c => c.ColumnId == columnId)
            .MaxAsync(c => (int?)c.Position) ?? -1;
    }

    public async Task<List<KanbanCard>> GetCardsAfterAsync(int columnId, int position)
    {
        return await _context.KanbanCards
            .Where(c => c.ColumnId == columnId && c.Position > position)
            .ToListAsync();
    }

    // ===== Card Labels =====

    public async Task AddCardLabelAsync(KanbanCardLabel cardLabel)
    {
        _context.KanbanCardLabels.Add(cardLabel);
        await _context.SaveChangesAsync();
    }

    public async Task AddCardLabelsAsync(IEnumerable<KanbanCardLabel> cardLabels)
    {
        _context.KanbanCardLabels.AddRange(cardLabels);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveCardLabelAsync(KanbanCardLabel cardLabel)
    {
        _context.KanbanCardLabels.Remove(cardLabel);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveCardLabelsAsync(IEnumerable<KanbanCardLabel> cardLabels)
    {
        _context.KanbanCardLabels.RemoveRange(cardLabels);
        await _context.SaveChangesAsync();
    }

    public async Task<List<KanbanCardLabel>> GetCardLabelsAsync(int cardId)
    {
        return await _context.KanbanCardLabels
            .Include(cl => cl.Label)
            .Where(cl => cl.CardId == cardId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task ClearCardLabelsAsync(int cardId)
    {
        var labels = await _context.KanbanCardLabels
            .Where(cl => cl.CardId == cardId)
            .ToListAsync();

        _context.KanbanCardLabels.RemoveRange(labels);
        await _context.SaveChangesAsync();
    }

    // ===== Comments =====

    public async Task<KanbanComment?> GetCommentByIdAsync(int id)
    {
        return await _context.KanbanComments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<KanbanComment>> GetCommentsByCardAsync(int cardId)
    {
        return await _context.KanbanComments
            .Include(c => c.Author)
            .Where(c => c.CardId == cardId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<KanbanComment> CreateCommentAsync(KanbanComment comment)
    {
        _context.KanbanComments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task UpdateCommentAsync(KanbanComment comment)
    {
        _context.KanbanComments.Update(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(KanbanComment comment)
    {
        _context.KanbanComments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    // ===== History =====

    public async Task<List<KanbanCardHistory>> GetHistoryByCardAsync(int cardId)
    {
        return await _context.KanbanCardHistories
            .Include(h => h.User)
            .Where(h => h.CardId == cardId)
            .OrderByDescending(h => h.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<KanbanCardHistory> CreateHistoryAsync(KanbanCardHistory history)
    {
        _context.KanbanCardHistories.Add(history);
        await _context.SaveChangesAsync();
        return history;
    }

    // ===== Labels =====

    public async Task<List<KanbanLabel>> GetLabelsByBoardAsync(int boardId)
    {
        return await _context.KanbanLabels
            .AsNoTracking()
            .Where(l => l.BoardId == boardId && l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<KanbanLabel?> GetLabelByIdAsync(int id)
    {
        return await _context.KanbanLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<KanbanLabel> CreateLabelAsync(KanbanLabel label)
    {
        _context.KanbanLabels.Add(label);
        await _context.SaveChangesAsync();
        return label;
    }

    public async Task UpdateLabelAsync(KanbanLabel label)
    {
        _context.KanbanLabels.Update(label);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteLabelAsync(KanbanLabel label)
    {
        _context.KanbanLabels.Remove(label);
        await _context.SaveChangesAsync();
    }

    // ===== Stats =====

    public async Task<KanbanStatsDto> GetStatsAsync(int boardId)
    {
        var cards = await _context.KanbanCards
            .Where(c => c.Column.BoardId == boardId && !c.IsArchived)
            .Select(c => new { c.CompletedAt, c.DueDate, c.Priority, c.AssignedUserId })
            .ToListAsync();

        var now = DateTime.UtcNow;
        return new KanbanStatsDto(
            TotalCards: cards.Count,
            CompletedCards: cards.Count(c => c.CompletedAt.HasValue),
            OverdueCards: cards.Count(c => c.DueDate.HasValue && c.DueDate < now && !c.CompletedAt.HasValue),
            HighPriorityCards: cards.Count(c => c.Priority >= KanbanPriority.High),
            UnassignedCards: cards.Count(c => !c.AssignedUserId.HasValue)
        );
    }
}
