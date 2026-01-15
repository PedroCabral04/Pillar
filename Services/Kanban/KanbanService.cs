using erp.DAOs.Kanban;
using erp.DTOs.Kanban;
using erp.Models.Identity;
using erp.Models.Kanban;
using erp.Services.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Kanban;

public class KanbanService : IKanbanService
{
    private readonly IKanbanDao _kanbanDao;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(
        IKanbanDao kanbanDao,
        UserManager<ApplicationUser> userManager,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<KanbanService> logger)
    {
        _kanbanDao = kanbanDao;
        _userManager = userManager;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    private static DateTime? ToUtc(DateTime? dt) => dt.HasValue
        ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)
        : null;

    private int GetRequiredTenantId()
    {
        return _tenantContextAccessor.Current?.TenantId ?? throw new InvalidOperationException("Tenant context is required");
    }

    private async Task<KanbanBoard> GetBoardWithOwnershipCheckAsync(int id, int userId)
    {
        var board = await _kanbanDao.GetBoardByIdAsync(id);
        if (board == null)
            throw new KeyNotFoundException($"Board {id} not found");

        if (board.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't own this board");

        return board;
    }

    // ===== Boards =====

    public async Task<List<KanbanBoardDto>> GetMyBoardsAsync(int userId)
    {
        var boards = await _kanbanDao.GetBoardsByOwnerAsync(userId);
        return boards.Select(b => new KanbanBoardDto(b.Id, b.Name, b.CreatedAt)).ToList();
    }

    public async Task<KanbanBoardDto> CreateBoardAsync(CreateBoardRequest req, int userId, int tenantId)
    {
        var board = new KanbanBoard { OwnerId = userId, Name = req.Name, TenantId = tenantId };
        var created = await _kanbanDao.CreateBoardAsync(board);

        // Create default columns
        await _kanbanDao.CreateColumnAsync(new KanbanColumn { BoardId = created.Id, Title = "A Fazer", Position = 0, TenantId = tenantId });
        await _kanbanDao.CreateColumnAsync(new KanbanColumn { BoardId = created.Id, Title = "Fazendo", Position = 1, TenantId = tenantId });
        await _kanbanDao.CreateColumnAsync(new KanbanColumn { BoardId = created.Id, Title = "Feito", Position = 2, TenantId = tenantId });

        return new KanbanBoardDto(created.Id, created.Name, created.CreatedAt);
    }

    public async Task<KanbanBoardDto?> GetBoardAsync(int id, int userId)
    {
        var board = await _kanbanDao.GetBoardByIdAsync(id);
        if (board == null || board.OwnerId != userId)
            return null;

        return new KanbanBoardDto(board.Id, board.Name, board.CreatedAt);
    }

    public async Task RenameBoardAsync(int id, string name, int userId)
    {
        var board = await GetBoardWithOwnershipCheckAsync(id, userId);
        board.Name = name;
        await _kanbanDao.UpdateBoardAsync(board);
    }

    public async Task DeleteBoardAsync(int id, int userId)
    {
        var board = await GetBoardWithOwnershipCheckAsync(id, userId);

        var boardCount = await _kanbanDao.GetBoardCountByOwnerAsync(userId);
        if (boardCount <= 1)
            throw new InvalidOperationException("Você precisa manter pelo menos um quadro.");

        await _kanbanDao.DeleteBoardAsync(board);
    }

    public async Task<KanbanBoardDto> GetOrCreateMyBoardAsync(int userId, int tenantId)
    {
        var board = (await _kanbanDao.GetBoardsByOwnerAsync(userId)).FirstOrDefault();
        if (board == null)
        {
            board = new KanbanBoard { OwnerId = userId, Name = "Meu quadro", TenantId = tenantId };
            board = await _kanbanDao.CreateBoardAsync(board);

            await _kanbanDao.CreateColumnAsync(new KanbanColumn { BoardId = board.Id, Title = "A Fazer", Position = 0, TenantId = tenantId });
            await _kanbanDao.CreateColumnAsync(new KanbanColumn { BoardId = board.Id, Title = "Fazendo", Position = 1, TenantId = tenantId });
            await _kanbanDao.CreateColumnAsync(new KanbanColumn { BoardId = board.Id, Title = "Feito", Position = 2, TenantId = tenantId });

            // Default labels
            await _kanbanDao.CreateLabelAsync(new KanbanLabel { BoardId = board.Id, Name = "Bug", Color = "#EF4444" });
            await _kanbanDao.CreateLabelAsync(new KanbanLabel { BoardId = board.Id, Name = "Feature", Color = "#22C55E" });
            await _kanbanDao.CreateLabelAsync(new KanbanLabel { BoardId = board.Id, Name = "Melhoria", Color = "#3B82F6" });
            await _kanbanDao.CreateLabelAsync(new KanbanLabel { BoardId = board.Id, Name = "Urgente", Color = "#F59E0B" });
        }

        return new KanbanBoardDto(board.Id, board.Name, board.CreatedAt);
    }

    // ===== Columns =====

    public async Task<List<ColumnWithCardsDto>> GetColumnsAsync(int? boardId, int userId)
    {
        var board = boardId.HasValue
            ? await _kanbanDao.GetBoardByIdAsync(boardId.Value)
            : (await _kanbanDao.GetBoardsByOwnerAsync(userId)).FirstOrDefault();

        if (board == null || board.OwnerId != userId)
            return new List<ColumnWithCardsDto>();

        var columns = await _kanbanDao.GetColumnsByBoardAsync(board.Id);
        var result = new List<ColumnWithCardsDto>();

        foreach (var col in columns)
        {
            var cards = await _kanbanDao.GetCardsByColumnAsync(col.Id, includeArchived: false);
            var cardDtos = new List<KanbanCardDto>();

            foreach (var card in cards)
            {
                var labels = (await _kanbanDao.GetCardLabelsAsync(card.Id))
                    .Select(cl => new KanbanLabelDto(cl.Label.Id, cl.Label.Name, cl.Label.Color))
                    .ToList();

                cardDtos.Add(new KanbanCardDto(
                    card.Id,
                    card.Title,
                    card.Description,
                    card.Position,
                    card.ColumnId,
                    card.CreatedAt,
                    card.DueDate,
                    card.Priority,
                    card.Color,
                    card.AssignedUserId,
                    card.AssignedUser?.FullName,
                    card.AssignedUser?.ProfilePhotoUrl,
                    card.CompletedAt,
                    card.IsArchived,
                    labels,
                    card.Comments.Count
                ));
            }

            result.Add(new ColumnWithCardsDto(col.Id, col.Title, col.Position, cardDtos.OrderBy(c => c.Position).ToList()));
        }

        return result;
    }

    public async Task<KanbanColumnDto> CreateColumnAsync(CreateColumnRequest req, int userId, int tenantId)
    {
        var board = (await _kanbanDao.GetBoardsByOwnerAsync(userId)).FirstOrDefault()
            ?? throw new InvalidOperationException("Crie o quadro primeiro");

        var maxPos = await _kanbanDao.GetMaxColumnPositionAsync(board.Id);
        var column = new KanbanColumn { BoardId = board.Id, Title = req.Title, Position = maxPos + 1, TenantId = tenantId };
        var created = await _kanbanDao.CreateColumnAsync(column);

        return new KanbanColumnDto(created.Id, created.Title, created.Position);
    }

    public async Task RenameColumnAsync(int id, string title, int userId)
    {
        var column = await _kanbanDao.GetColumnByIdAsync(id) ?? throw new InvalidOperationException();
        var board = await _kanbanDao.GetBoardByIdAsync(column.BoardId);
        if (board == null || board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        column.Title = title;
        await _kanbanDao.UpdateColumnAsync(column);
    }

    public async Task DeleteColumnAsync(int id, int userId)
    {
        var column = await _kanbanDao.GetColumnByIdAsync(id) ?? throw new InvalidOperationException();
        var board = await _kanbanDao.GetBoardByIdAsync(column.BoardId);
        if (board == null || board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        var hasCards = (await _kanbanDao.GetCardsByColumnAsync(id)).Any();
        if (hasCards)
            throw new InvalidOperationException("Não é possível excluir uma coluna com cards.");

        // Shift positions
        var laterCols = await _kanbanDao.GetColumnsAfterAsync(column.BoardId, column.Position);
        foreach (var c in laterCols) c.Position--;

        await _kanbanDao.DeleteColumnAsync(column);
    }

    public async Task ReorderColumnAsync(int columnId, int newPosition, int userId)
    {
        var column = await _kanbanDao.GetColumnByIdAsync(columnId) ?? throw new InvalidOperationException();
        var board = await _kanbanDao.GetBoardByIdAsync(column.BoardId);
        if (board == null || board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        var cols = await _kanbanDao.GetColumnsByBoardAsync(column.BoardId);
        var oldPos = column.Position;
        newPosition = Math.Clamp(newPosition, 0, cols.Count - 1);

        if (newPosition == oldPos) return;

        if (newPosition > oldPos)
        {
            foreach (var c in cols.Where(c => c.Position > oldPos && c.Position <= newPosition))
                c.Position--;
        }
        else
        {
            foreach (var c in cols.Where(c => c.Position >= newPosition && c.Position < oldPos))
                c.Position++;
        }

        column.Position = newPosition;

        foreach (var c in cols) await _kanbanDao.UpdateColumnAsync(c);
    }

    // ===== Cards =====

    public async Task<KanbanCardDto?> GetCardAsync(int id, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(id);
        if (card == null || card.Column.Board.OwnerId != userId)
            return null;

        var labels = (await _kanbanDao.GetCardLabelsAsync(card.Id))
            .Select(cl => new KanbanLabelDto(cl.Label.Id, cl.Label.Name, cl.Label.Color))
            .ToList();

        return new KanbanCardDto(
            card.Id,
            card.Title,
            card.Description,
            card.Position,
            card.ColumnId,
            card.CreatedAt,
            card.DueDate,
            card.Priority,
            card.Color,
            card.AssignedUserId,
            card.AssignedUser?.FullName,
            card.AssignedUser?.ProfilePhotoUrl,
            card.CompletedAt,
            card.IsArchived,
            labels,
            card.Comments.Count
        );
    }

    public async Task<KanbanCardDto> CreateCardAsync(CreateCardRequest req, int userId, int tenantId)
    {
        var column = await _kanbanDao.GetColumnByIdAsync(req.ColumnId);
        if (column == null) throw new InvalidOperationException("Coluna não encontrada");

        var board = await _kanbanDao.GetBoardByIdAsync(column.BoardId);
        if (board == null || board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        var maxPos = await _kanbanDao.GetMaxCardPositionAsync(column.Id);
        var card = new KanbanCard
        {
            ColumnId = column.Id,
            Title = req.Title,
            Description = req.Description,
            Position = maxPos + 1,
            DueDate = ToUtc(req.DueDate),
            Priority = req.Priority,
            AssignedUserId = req.AssignedUserId,
            Color = req.Color,
            TenantId = tenantId
        };

        card = await _kanbanDao.CreateCardAsync(card);

        // Add labels
        if (req.LabelIds?.Any() == true)
        {
            var allLabels = await _kanbanDao.GetLabelsByBoardAsync(column.BoardId);
            var validLabelIds = allLabels.Where(l => req.LabelIds.Contains(l.Id)).Select(l => l.Id).ToList();

            var newLabels = validLabelIds.Select(labelId => new KanbanCardLabel { CardId = card.Id, LabelId = labelId }).ToList();
            if (newLabels.Any())
                await _kanbanDao.AddCardLabelsAsync(newLabels);
        }

        // Add history
        await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
        {
            CardId = card.Id,
            UserId = userId,
            Action = KanbanHistoryAction.Created,
            Description = "criou o card"
        });

        // Reload for response
        card = await _kanbanDao.GetCardByIdAsync(card.Id) ?? throw new InvalidOperationException();
        var labels = (await _kanbanDao.GetCardLabelsAsync(card.Id))
            .Select(cl => new KanbanLabelDto(cl.Label.Id, cl.Label.Name, cl.Label.Color))
            .ToList();

        return new KanbanCardDto(
            card.Id, card.Title, card.Description, card.Position, card.ColumnId,
            card.CreatedAt, card.DueDate, card.Priority, card.Color,
            card.AssignedUserId, card.AssignedUser?.FullName, card.AssignedUser?.ProfilePhotoUrl,
            card.CompletedAt, card.IsArchived, labels, 0
        );
    }

    public async Task UpdateCardAsync(int id, UpdateCardRequest req, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(id) ?? throw new InvalidOperationException();
        if (card.Column.Board.OwnerId != userId) throw new UnauthorizedAccessException();

        var changes = new List<string>();
        if (card.Title != req.Title) changes.Add($"título: '{card.Title}' → '{req.Title}'");
        if (card.Description != req.Description) changes.Add("descrição atualizada");
        if (card.DueDate != req.DueDate) changes.Add(req.DueDate.HasValue ? $"prazo definido: {req.DueDate:dd/MM/yyyy}" : "prazo removido");
        if (card.Priority != req.Priority) changes.Add($"prioridade: {card.Priority} → {req.Priority}");
        if (card.AssignedUserId != req.AssignedUserId) changes.Add("responsável alterado");
        if (card.Color != req.Color) changes.Add("cor alterada");
        if (card.CompletedAt != req.CompletedAt)
        {
            if (req.CompletedAt.HasValue && !card.CompletedAt.HasValue) changes.Add("card concluído");
            else if (!req.CompletedAt.HasValue && card.CompletedAt.HasValue) changes.Add("card reaberto");
        }

        card.Title = req.Title;
        card.Description = req.Description;
        card.DueDate = ToUtc(req.DueDate);
        card.Priority = req.Priority;
        card.AssignedUserId = req.AssignedUserId;
        card.Color = req.Color;
        card.CompletedAt = ToUtc(req.CompletedAt);

        // Update labels
        if (req.LabelIds != null)
        {
            var currentLabelIds = (await _kanbanDao.GetCardLabelsAsync(id)).Select(cl => cl.LabelId).ToList();
            var toRemove = (await _kanbanDao.GetCardLabelsAsync(id)).Where(cl => !req.LabelIds.Contains(cl.LabelId)).ToList();
            var toAdd = req.LabelIds.Except(currentLabelIds).ToList();

            if (toRemove.Any())
                await _kanbanDao.RemoveCardLabelsAsync(toRemove);

            var allLabels = await _kanbanDao.GetLabelsByBoardAsync(card.Column.BoardId);
            var labelsToAdd = toAdd
                .Where(allLabels.Select(l => l.Id).Contains)
                .Select(labelId => new KanbanCardLabel { CardId = id, LabelId = labelId })
                .ToList();

            if (labelsToAdd.Any())
                await _kanbanDao.AddCardLabelsAsync(labelsToAdd);

            if (toRemove.Any() || toAdd.Any()) changes.Add("etiquetas atualizadas");
        }

        await _kanbanDao.UpdateCardAsync(card);

        if (changes.Any())
        {
            await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
            {
                CardId = id,
                UserId = userId,
                Action = KanbanHistoryAction.Updated,
                Description = string.Join("; ", changes)
            });
        }
    }

    public async Task DeleteCardAsync(int id, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(id) ?? throw new InvalidOperationException();
        if (card.Column.Board.OwnerId != userId) throw new UnauthorizedAccessException();

        var laterCards = await _kanbanDao.GetCardsAfterAsync(card.ColumnId, card.Position);
        foreach (var c in laterCards) c.Position--;

        await _kanbanDao.DeleteCardAsync(card);
    }

    public async Task ArchiveCardAsync(int id, bool isArchived, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(id) ?? throw new InvalidOperationException();
        if (card.Column.Board.OwnerId != userId) throw new UnauthorizedAccessException();

        card.IsArchived = isArchived;
        await _kanbanDao.UpdateCardAsync(card);

        await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
        {
            CardId = id,
            UserId = userId,
            Action = isArchived ? KanbanHistoryAction.Archived : KanbanHistoryAction.Restored,
            Description = isArchived ? "arquivou o card" : "restaurou o card"
        });
    }

    public async Task MoveCardAsync(MoveCardRequest req, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(req.CardId) ?? throw new InvalidOperationException();
        if (card.Column.Board.OwnerId != userId) throw new UnauthorizedAccessException();

        var fromColumn = card.Column;
        var targetColumn = await _kanbanDao.GetColumnByIdAsync(req.ToColumnId);
        if (targetColumn == null || targetColumn.BoardId != fromColumn.BoardId)
            throw new UnauthorizedAccessException();

        var targetCards = await _kanbanDao.GetCardsByColumnAsync(req.ToColumnId);
        var newPos = Math.Clamp(req.ToPosition, 0, targetCards.Count);

        if (card.ColumnId != req.ToColumnId)
        {
            var oldColumnCards = await _kanbanDao.GetCardsAfterAsync(card.ColumnId, card.Position);
            foreach (var c in oldColumnCards) c.Position--;

            card.ColumnId = req.ToColumnId;
            await _kanbanDao.UpdateCardAsync(card);

            await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
            {
                CardId = card.Id,
                UserId = userId,
                Action = KanbanHistoryAction.Moved,
                Description = $"moveu de '{fromColumn.Title}' para '{targetColumn.Title}'",
                OldValue = fromColumn.Title,
                NewValue = targetColumn.Title
            });
        }

        var allTargetCards = await _kanbanDao.GetCardsByColumnAsync(req.ToColumnId);
        foreach (var c in allTargetCards.Where(c => c.Position >= newPos)) c.Position++;

        card.Position = newPos;
        await _kanbanDao.UpdateCardAsync(card);
    }

    // ===== Comments =====

    public async Task<List<KanbanCommentDto>> GetCommentsAsync(int cardId, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(cardId);
        if (card == null || card.Column.Board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        var comments = await _kanbanDao.GetCommentsByCardAsync(cardId);
        return comments.Select(c => new KanbanCommentDto(
            c.Id, c.CardId, c.AuthorId, c.Author.FullName, c.Author.ProfilePhotoUrl,
            c.Content, c.CreatedAt, c.UpdatedAt, c.IsEdited
        )).ToList();
    }

    public async Task<KanbanCommentDto> CreateCommentAsync(int cardId, CreateCommentRequest req, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(cardId);
        if (card == null || card.Column.Board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        var comment = new KanbanComment
        {
            CardId = cardId,
            AuthorId = userId,
            Content = req.Content
        };

        comment = await _kanbanDao.CreateCommentAsync(comment);

        await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
        {
            CardId = cardId,
            UserId = userId,
            Action = KanbanHistoryAction.CommentAdded,
            Description = "adicionou um comentário"
        });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        return new KanbanCommentDto(
            comment.Id, comment.CardId, comment.AuthorId, user?.FullName ?? "", user?.ProfilePhotoUrl,
            comment.Content, comment.CreatedAt, comment.UpdatedAt, comment.IsEdited
        );
    }

    public async Task UpdateCommentAsync(int cardId, int commentId, UpdateCommentRequest req, int userId)
    {
        var comment = await _kanbanDao.GetCommentByIdAsync(commentId) ?? throw new InvalidOperationException();
        var card = await _kanbanDao.GetCardByIdAsync(cardId);

        if (comment.AuthorId != userId && (card == null || card.Column.Board.OwnerId != userId))
            throw new UnauthorizedAccessException();

        comment.Content = req.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        comment.IsEdited = true;

        await _kanbanDao.UpdateCommentAsync(comment);

        await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
        {
            CardId = cardId,
            UserId = userId,
            Action = KanbanHistoryAction.CommentEdited,
            Description = "editou um comentário"
        });
    }

    public async Task DeleteCommentAsync(int cardId, int commentId, int userId)
    {
        var comment = await _kanbanDao.GetCommentByIdAsync(commentId) ?? throw new InvalidOperationException();
        var card = await _kanbanDao.GetCardByIdAsync(cardId);

        if (comment.AuthorId != userId && (card == null || card.Column.Board.OwnerId != userId))
            throw new UnauthorizedAccessException();

        await _kanbanDao.DeleteCommentAsync(comment);

        await _kanbanDao.CreateHistoryAsync(new KanbanCardHistory
        {
            CardId = cardId,
            UserId = userId,
            Action = KanbanHistoryAction.CommentDeleted,
            Description = "removeu um comentário"
        });
    }

    // ===== History =====

    public async Task<List<KanbanCardHistoryDto>> GetHistoryAsync(int cardId, int userId)
    {
        var card = await _kanbanDao.GetCardByIdAsync(cardId);
        if (card == null || card.Column.Board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        var history = await _kanbanDao.GetHistoryByCardAsync(cardId);
        return history.Select(h => new KanbanCardHistoryDto(
            h.Id, h.CardId, h.UserId, h.User?.FullName ?? "Usuário", h.Action.ToString(),
            h.Description ?? string.Empty, h.OldValue, h.NewValue, h.CreatedAt
        )).ToList();
    }

    // ===== Labels =====

    public async Task<List<KanbanLabelDto>> GetLabelsAsync(int? boardId, int userId)
    {
        var board = boardId.HasValue
            ? await _kanbanDao.GetBoardByIdAsync(boardId.Value)
            : (await _kanbanDao.GetBoardsByOwnerAsync(userId)).FirstOrDefault();

        if (board == null) return new List<KanbanLabelDto>();

        var labels = await _kanbanDao.GetLabelsByBoardAsync(board.Id);
        return labels.Select(l => new KanbanLabelDto(l.Id, l.Name, l.Color)).ToList();
    }

    public async Task<KanbanLabelDto> CreateLabelAsync(CreateLabelRequest req, int userId)
    {
        var board = req.BoardId.HasValue
            ? await _kanbanDao.GetBoardByIdAsync(req.BoardId.Value)
            : (await _kanbanDao.GetBoardsByOwnerAsync(userId)).FirstOrDefault();

        if (board == null) throw new InvalidOperationException("Crie o quadro primeiro");

        var label = new KanbanLabel { BoardId = board.Id, Name = req.Name, Color = req.Color };
        var created = await _kanbanDao.CreateLabelAsync(label);

        return new KanbanLabelDto(created.Id, created.Name, created.Color);
    }

    public async Task UpdateLabelAsync(int id, UpdateLabelRequest req, int userId)
    {
        var label = await _kanbanDao.GetLabelByIdAsync(id);
        if (label == null) throw new InvalidOperationException();

        var board = await _kanbanDao.GetBoardByIdAsync(label.BoardId);
        if (board == null || board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        label.Name = req.Name;
        label.Color = req.Color;
        await _kanbanDao.UpdateLabelAsync(label);
    }

    public async Task DeleteLabelAsync(int id, int userId)
    {
        var label = await _kanbanDao.GetLabelByIdAsync(id);
        if (label == null) throw new InvalidOperationException();

        var board = await _kanbanDao.GetBoardByIdAsync(label.BoardId);
        if (board == null || board.OwnerId != userId)
            throw new UnauthorizedAccessException();

        label.IsActive = false;
        await _kanbanDao.UpdateLabelAsync(label);
    }

    // ===== Stats =====

    public async Task<KanbanStatsDto> GetStatsAsync(int? boardId, int userId)
    {
        var board = boardId.HasValue
            ? await _kanbanDao.GetBoardByIdAsync(boardId.Value)
            : (await _kanbanDao.GetBoardsByOwnerAsync(userId)).FirstOrDefault();

        if (board == null || board.OwnerId != userId)
            return new KanbanStatsDto(0, 0, 0, 0, 0);

        return await _kanbanDao.GetStatsAsync(board.Id);
    }

    // ===== Users =====

    public async Task<List<AssignableUserDto>> GetAssignableUsersAsync()
    {
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, Photo = u.ProfilePhotoUrl })
            .Take(100)
            .ToListAsync();

        return users.Select(u => new AssignableUserDto(u.Id, u.FullName ?? "", u.Photo)).ToList();
    }
}
