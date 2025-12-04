using System.Linq;
using System.Threading.Tasks;
using erp.Data;
using erp.DTOs.Kanban;
using erp.Models.Identity;
using erp.Models.Kanban;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace erp.Controllers;

[ApiController]
[Route("api/kanban")]
[Authorize]
public class KanbanController(ApplicationDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    // PostgreSQL requires DateTime with Kind=Utc for timestamp with time zone columns
    private static DateTime? ToUtc(DateTime? dt) => dt.HasValue 
        ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) 
        : null;

    private async Task<int> GetMyUserIdAsync()
    {
        var u = await users.GetUserAsync(User);
        if (u is null) throw new UnauthorizedAccessException();
        return u.Id;
    }

    private async Task<ApplicationUser> GetMyUserAsync()
    {
        var u = await users.GetUserAsync(User);
        if (u is null) throw new UnauthorizedAccessException();
        return u;
    }

    // ===== Boards (Multiple) =====

    /// <summary>
    /// Obtém a lista de quadros (boards) pertencentes ao usuário autenticado.
    /// </summary>
    /// <returns>Uma lista de `KanbanBoardDto` contendo os quadros do usuário.</returns>
    [HttpGet("boards")]
    public async Task<ActionResult<List<KanbanBoardDto>>> GetMyBoards()
    {
        var myId = await GetMyUserIdAsync();
        var boards = await db.KanbanBoards
            .Where(b => b.OwnerId == myId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new KanbanBoardDto(b.Id, b.Name, b.CreatedAt))
            .ToListAsync();

        return Ok(boards);
    }

    /// <summary>
    /// Cria um novo quadro para o usuário autenticado e adiciona colunas padrão.
    /// </summary>
    /// <param name="req">Dados necessários para criar o quadro (`CreateBoardRequest`).</param>
    /// <returns>O `KanbanBoardDto` criado com código HTTP 201.</returns>
    [HttpPost("boards")]
    public async Task<ActionResult<KanbanBoardDto>> CreateBoard([FromBody] CreateBoardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        
        var board = new KanbanBoard { OwnerId = myId, Name = req.Name };
        db.KanbanBoards.Add(board);
        await db.SaveChangesAsync();

        // Default columns
        db.KanbanColumns.AddRange(
            new KanbanColumn { BoardId = board.Id, Title = "A Fazer", Position = 0 },
            new KanbanColumn { BoardId = board.Id, Title = "Fazendo", Position = 1 },
            new KanbanColumn { BoardId = board.Id, Title = "Feito", Position = 2 }
        );

        await db.SaveChangesAsync();

        return Created($"/api/kanban/boards/{board.Id}", new KanbanBoardDto(board.Id, board.Name, board.CreatedAt));
    }

    /// <summary>
    /// Retorna um quadro específico do usuário autenticado.
    /// </summary>
    /// <param name="id">ID do quadro a ser recuperado.</param>
    /// <returns>O `KanbanBoardDto` correspondente ou 404 se não encontrado.</returns>
    [HttpGet("boards/{id}")]
    public async Task<ActionResult<KanbanBoardDto>> GetBoard(int id)
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == myId);
        if (board is null) return NotFound();

        return Ok(new KanbanBoardDto(board.Id, board.Name, board.CreatedAt));
    }

    /// <summary>
    /// Renomeia um quadro do usuário autenticado.
    /// </summary>
    /// <param name="id">ID do quadro a ser renomeado.</param>
    /// <param name="req">Objeto `RenameBoardRequest` contendo o novo nome.</param>
    /// <returns>204 No Content em sucesso, 404 se o quadro não existir.</returns>
    [HttpPut("boards/{id}/name")]
    public async Task<IActionResult> RenameBoard(int id, [FromBody] RenameBoardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == myId);
        if (board is null) return NotFound();

        board.Name = req.Name;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Exclui um quadro do usuário autenticado (remove também colunas, cards e dados relacionados).
    /// </summary>
    /// <param name="id">ID do quadro a ser excluído.</param>
    /// <returns>204 No Content em sucesso, 400 se for o último quadro, 404 se não encontrado.</returns>
    [HttpDelete("boards/{id}")]
    public async Task<IActionResult> DeleteBoard(int id)
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == myId);
        if (board is null) return NotFound();

        // Check if this is the only board
        var boardCount = await db.KanbanBoards.CountAsync(b => b.OwnerId == myId);
        if (boardCount <= 1)
        {
            return BadRequest("Você precisa manter pelo menos um quadro.");
        }

        // Delete all related data
        var columns = await db.KanbanColumns.Where(c => c.BoardId == id).ToListAsync();
        var columnIds = columns.Select(c => c.Id).ToList();
        
        var cards = await db.KanbanCards.Where(c => columnIds.Contains(c.ColumnId)).ToListAsync();
        var cardIds = cards.Select(c => c.Id).ToList();

        // Delete card-related data
        var cardLabels = await db.KanbanCardLabels.Where(cl => cardIds.Contains(cl.CardId)).ToListAsync();
        var comments = await db.KanbanComments.Where(c => cardIds.Contains(c.CardId)).ToListAsync();
        var history = await db.KanbanCardHistories.Where(h => cardIds.Contains(h.CardId)).ToListAsync();

        db.KanbanCardLabels.RemoveRange(cardLabels);
        db.KanbanComments.RemoveRange(comments);
        db.KanbanCardHistories.RemoveRange(history);
        db.KanbanCards.RemoveRange(cards);
        
        // Delete labels and columns
        var labels = await db.KanbanLabels.Where(l => l.BoardId == id).ToListAsync();
        db.KanbanLabels.RemoveRange(labels);
        db.KanbanColumns.RemoveRange(columns);

        db.KanbanBoards.Remove(board);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ===== Board (Legacy - get or create default) =====

    /// <summary>
    /// Retorna o quadro do usuário ou cria um quadro padrão caso não exista.
    /// </summary>
    /// <returns>O `KanbanBoardDto` do quadro existente ou recém-criado.</returns>
    [HttpGet("board")]
    public async Task<ActionResult<KanbanBoardDto>> GetOrCreateMyBoard()
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        if (board is null)
        {
            board = new KanbanBoard { OwnerId = myId, Name = "Meu quadro" };
            db.KanbanBoards.Add(board);
            await db.SaveChangesAsync();

            // Default columns
            db.KanbanColumns.AddRange(
                new KanbanColumn { BoardId = board.Id, Title = "A Fazer", Position = 0 },
                new KanbanColumn { BoardId = board.Id, Title = "Fazendo", Position = 1 },
                new KanbanColumn { BoardId = board.Id, Title = "Feito", Position = 2 }
            );

            // Default labels
            db.KanbanLabels.AddRange(
                new KanbanLabel { BoardId = board.Id, Name = "Bug", Color = "#EF4444" },
                new KanbanLabel { BoardId = board.Id, Name = "Feature", Color = "#22C55E" },
                new KanbanLabel { BoardId = board.Id, Name = "Melhoria", Color = "#3B82F6" },
                new KanbanLabel { BoardId = board.Id, Name = "Urgente", Color = "#F59E0B" }
            );

            await db.SaveChangesAsync();
        }
        return Ok(new KanbanBoardDto(board.Id, board.Name, board.CreatedAt));
    }

    /// <summary>
    /// Obtém as colunas (e seus cards) de um quadro. Se `boardId` não for fornecido, usa o quadro padrão do usuário.
    /// </summary>
    /// <param name="boardId">ID opcional do quadro para filtrar as colunas.</param>
    /// <returns>Objeto contendo a lista de colunas e cards formatados para a UI.</returns>
    [HttpGet("columns")]
    public async Task<ActionResult<object>> GetColumns([FromQuery] int? boardId = null)
    {
        var myId = await GetMyUserIdAsync();
        
        KanbanBoard? board;
        if (boardId.HasValue)
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == boardId.Value && b.OwnerId == myId);
        }
        else
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        }
        
        if (board is null) return Ok(new { columns = Array.Empty<object>() });

        // Load columns with all related data - use single filtered include then chain ThenIncludes
        var cols = await db.KanbanColumns
            .Where(c => c.BoardId == board.Id)
            .OrderBy(c => c.Position)
            .Include(c => c.Cards.Where(t => !t.IsArchived))
                .ThenInclude(t => t.AssignedUser)
            .Include(c => c.Cards)
                .ThenInclude(t => t.CardLabels)
                    .ThenInclude(cl => cl.Label)
            .Include(c => c.Cards)
                .ThenInclude(t => t.Comments)
            .AsSplitQuery()
            .ToListAsync();

        // Project to DTOs
        var result = cols.Select(c => new
        {
            c.Id,
            c.Title,
            c.Position,
            cards = c.Cards.Where(t => !t.IsArchived).OrderBy(t => t.Position).Select(t => new KanbanCardDto(
                t.Id,
                t.Title,
                t.Description,
                t.Position,
                t.ColumnId,
                t.CreatedAt,
                t.DueDate,
                t.Priority,
                t.Color,
                t.AssignedUserId,
                t.AssignedUser?.FullName,
                t.AssignedUser?.ProfilePhotoUrl,
                t.CompletedAt,
                t.IsArchived,
                t.CardLabels.Select(cl => new KanbanLabelDto(cl.Label.Id, cl.Label.Name, cl.Label.Color)).ToList(),
                t.Comments.Count
            )).ToList()
        }).ToList();

        return Ok(new { columns = result });
    }

    /// <summary>
    /// Calcula e retorna estatísticas do quadro (total de cards, concluídos, atrasados, prioridades, não atribuídos).
    /// </summary>
    /// <param name="boardId">ID opcional do quadro para calcular as estatísticas.</param>
    /// <returns>Um `KanbanStatsDto` com os valores agregados do quadro.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<KanbanStatsDto>> GetStats([FromQuery] int? boardId = null)
    {
        var myId = await GetMyUserIdAsync();
        
        KanbanBoard? board;
        if (boardId.HasValue)
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == boardId.Value && b.OwnerId == myId);
        }
        else
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        }
        
        if (board is null) return Ok(new KanbanStatsDto(0, 0, 0, 0, 0));

        var cardIds = await db.KanbanColumns
            .Where(c => c.BoardId == board.Id)
            .SelectMany(c => c.Cards)
            .Where(c => !c.IsArchived)
            .Select(c => new { c.Id, c.CompletedAt, c.DueDate, c.Priority, c.AssignedUserId })
            .ToListAsync();

        var now = DateTime.UtcNow;
        return Ok(new KanbanStatsDto(
            TotalCards: cardIds.Count,
            CompletedCards: cardIds.Count(c => c.CompletedAt.HasValue),
            OverdueCards: cardIds.Count(c => c.DueDate.HasValue && c.DueDate < now && !c.CompletedAt.HasValue),
            HighPriorityCards: cardIds.Count(c => c.Priority >= KanbanPriority.High),
            UnassignedCards: cardIds.Count(c => !c.AssignedUserId.HasValue)
        ));
    }

    // ===== Cards =====

    /// <summary>
    /// Recupera um card pelo seu ID, incluindo labels e contagem de comentários.
    /// </summary>
    /// <param name="id">ID do card a ser recuperado.</param>
    /// <returns>O `KanbanCardDto` com os detalhes do card ou 404/403 conforme aplicável.</returns>
    [HttpGet("cards/{id}")]
    public async Task<ActionResult<KanbanCardDto>> GetCard(int id)
    {
        var myId = await GetMyUserIdAsync();
        var card = await db.KanbanCards
            .Include(t => t.Column).ThenInclude(c => c.Board)
            .Include(t => t.AssignedUser)
            .Include(t => t.CardLabels).ThenInclude(cl => cl.Label)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != myId) return Forbid();

        return Ok(new KanbanCardDto(
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
            card.CardLabels.Select(cl => new KanbanLabelDto(cl.Label.Id, cl.Label.Name, cl.Label.Color)).ToList(),
            card.Comments.Count
        ));
    }

    /// <summary>
    /// Cria um novo card em uma coluna específica e aplica labels e histórico inicial.
    /// </summary>
    /// <param name="req">`CreateCardRequest` com dados do card (coluna, título, descrição, labels, etc.).</param>
    /// <returns>O `KanbanCardDto` criado com código HTTP 201.</returns>
    [HttpPost("cards")]
    public async Task<ActionResult<KanbanCardDto>> CreateCard([FromBody] CreateCardRequest req)
    {
        var user = await GetMyUserAsync();
        var column = await db.KanbanColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == req.ColumnId);
        if (column is null) return NotFound("Coluna não encontrada");
        if (column.Board.OwnerId != user.Id) return Forbid();

        var maxPos = await db.KanbanCards.Where(t => t.ColumnId == column.Id).MaxAsync(t => (int?)t.Position) ?? -1;
        var card = new KanbanCard
        {
            ColumnId = column.Id,
            Title = req.Title,
            Description = req.Description,
            Position = maxPos + 1,
            DueDate = ToUtc(req.DueDate),
            Priority = req.Priority,
            AssignedUserId = req.AssignedUserId,
            Color = req.Color
        };
        db.KanbanCards.Add(card);
        await db.SaveChangesAsync();

        // Add labels
        if (req.LabelIds?.Any() == true)
        {
            var validLabels = await db.KanbanLabels
                .Where(l => l.BoardId == column.BoardId && req.LabelIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();

            foreach (var labelId in validLabels)
            {
                db.KanbanCardLabels.Add(new KanbanCardLabel { CardId = card.Id, LabelId = labelId });
            }
            await db.SaveChangesAsync();
        }

        // Add history
        db.KanbanCardHistories.Add(new KanbanCardHistory
        {
            CardId = card.Id,
            UserId = user.Id,
            Action = KanbanHistoryAction.Created,
            Description = "criou o card"
        });
        await db.SaveChangesAsync();

        // Reload with relationships
        var labels = await db.KanbanCardLabels
            .Where(cl => cl.CardId == card.Id)
            .Include(cl => cl.Label)
            .Select(cl => new KanbanLabelDto(cl.Label.Id, cl.Label.Name, cl.Label.Color))
            .ToListAsync();

        ApplicationUser? assignedUser = null;
        if (card.AssignedUserId.HasValue)
        {
            assignedUser = await users.FindByIdAsync(card.AssignedUserId.Value.ToString());
        }

        return Created($"/api/kanban/cards/{card.Id}", new KanbanCardDto(
            card.Id, card.Title, card.Description, card.Position, card.ColumnId,
            card.CreatedAt, card.DueDate, card.Priority, card.Color,
            card.AssignedUserId, assignedUser?.FullName, assignedUser?.ProfilePhotoUrl,
            card.CompletedAt, card.IsArchived, labels, 0
        ));
    }

    /// <summary>
    /// Atualiza os dados de um card (título, descrição, prazo, prioridade, responsável, etiquetas, conclusão).
    /// </summary>
    /// <param name="id">ID do card a ser atualizado.</param>
    /// <param name="req">`UpdateCardRequest` com os campos a serem atualizados.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPut("cards/{id}")]
    public async Task<IActionResult> UpdateCard(int id, [FromBody] UpdateCardRequest req)
    {
        var user = await GetMyUserAsync();
        var card = await db.KanbanCards
            .Include(t => t.Column).ThenInclude(c => c.Board)
            .Include(t => t.CardLabels)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != user.Id) return Forbid();

        var changes = new List<string>();

        // Track changes for history
        if (card.Title != req.Title) changes.Add($"título: '{card.Title}' → '{req.Title}'");
        if (card.Description != req.Description) changes.Add("descrição atualizada");
        if (card.DueDate != req.DueDate) 
            changes.Add(req.DueDate.HasValue ? $"prazo definido: {req.DueDate:dd/MM/yyyy}" : "prazo removido");
        if (card.Priority != req.Priority) changes.Add($"prioridade: {card.Priority} → {req.Priority}");
        if (card.AssignedUserId != req.AssignedUserId) changes.Add("responsável alterado");
        if (card.Color != req.Color) changes.Add("cor alterada");
        if (card.CompletedAt != req.CompletedAt)
        {
            if (req.CompletedAt.HasValue && !card.CompletedAt.HasValue)
                changes.Add("card concluído");
            else if (!req.CompletedAt.HasValue && card.CompletedAt.HasValue)
                changes.Add("card reaberto");
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
            var currentLabelIds = card.CardLabels.Select(cl => cl.LabelId).ToList();
            var toRemove = card.CardLabels.Where(cl => !req.LabelIds.Contains(cl.LabelId)).ToList();
            var toAdd = req.LabelIds.Except(currentLabelIds).ToList();

            db.KanbanCardLabels.RemoveRange(toRemove);
            
            var validNewLabels = await db.KanbanLabels
                .Where(l => l.BoardId == card.Column.BoardId && toAdd.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();

            foreach (var labelId in validNewLabels)
            {
                db.KanbanCardLabels.Add(new KanbanCardLabel { CardId = card.Id, LabelId = labelId });
            }

            if (toRemove.Any() || validNewLabels.Any())
                changes.Add("etiquetas atualizadas");
        }

        if (changes.Any())
        {
            db.KanbanCardHistories.Add(new KanbanCardHistory
            {
                CardId = card.Id,
                UserId = user.Id,
                Action = KanbanHistoryAction.Updated,
                Description = string.Join("; ", changes)
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Exclui um card e reajusta posições dos demais cards na coluna.
    /// </summary>
    /// <param name="id">ID do card a ser excluído.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpDelete("cards/{id}")]
    public async Task<IActionResult> DeleteCard(int id)
    {
        var myId = await GetMyUserIdAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == id);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != myId) return Forbid();

        var laterCards = await db.KanbanCards
            .Where(t => t.ColumnId == card.ColumnId && t.Position > card.Position)
            .ToListAsync();
        foreach (var c in laterCards) c.Position--;

        db.KanbanCards.Remove(card);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Arquiva ou restaura um card (marca `IsArchived`).
    /// </summary>
    /// <param name="id">ID do card a ser arquivado/restaurado.</param>
    /// <param name="req">`ArchiveCardRequest` contendo `IsArchived`.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPost("cards/{id}/archive")]
    public async Task<IActionResult> ArchiveCard(int id, [FromBody] ArchiveCardRequest req)
    {
        var user = await GetMyUserAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == id);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != user.Id) return Forbid();

        card.IsArchived = req.IsArchived;

        db.KanbanCardHistories.Add(new KanbanCardHistory
        {
            CardId = card.Id,
            UserId = user.Id,
            Action = req.IsArchived ? KanbanHistoryAction.Archived : KanbanHistoryAction.Restored,
            Description = req.IsArchived ? "arquivou o card" : "restaurou o card"
        });

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Move um card entre colunas e/ou atualiza sua posição dentro da coluna.
    /// </summary>
    /// <param name="req">`MoveCardRequest` com `CardId`, `ToColumnId` e `ToPosition`.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPost("cards/move")]
    public async Task<IActionResult> MoveCard([FromBody] MoveCardRequest req)
    {
        var user = await GetMyUserAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == req.CardId);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != user.Id) return Forbid();

        var fromColumn = card.Column;
        var targetColumn = await db.KanbanColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == req.ToColumnId);
        if (targetColumn is null || targetColumn.Board.OwnerId != user.Id) return Forbid();

        var targetCards = await db.KanbanCards.Where(t => t.ColumnId == req.ToColumnId).OrderBy(t => t.Position).ToListAsync();
        var newPos = Math.Clamp(req.ToPosition, 0, targetCards.Count);

        if (card.ColumnId != req.ToColumnId)
        {
            var oldColumnCards = await db.KanbanCards.Where(t => t.ColumnId == card.ColumnId && t.Position > card.Position).ToListAsync();
            foreach (var c in oldColumnCards) c.Position--;
            card.ColumnId = req.ToColumnId;
            await db.SaveChangesAsync();
            targetCards = await db.KanbanCards.Where(t => t.ColumnId == req.ToColumnId).OrderBy(t => t.Position).ToListAsync();

            db.KanbanCardHistories.Add(new KanbanCardHistory
            {
                CardId = card.Id,
                UserId = user.Id,
                Action = KanbanHistoryAction.Moved,
                Description = $"moveu de '{fromColumn.Title}' para '{targetColumn.Title}'",
                OldValue = fromColumn.Title,
                NewValue = targetColumn.Title
            });
        }

        foreach (var t in targetCards.Where(t => t.Position >= newPos)) t.Position++;
        card.Position = newPos;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Comments =====

    /// <summary>
    /// Obtém os comentários de um card, ordenados por data decrescente.
    /// </summary>
    /// <param name="cardId">ID do card cujo comentários serão retornados.</param>
    /// <returns>Lista de `KanbanCommentDto` do card.</returns>
    [HttpGet("cards/{cardId}/comments")]
    public async Task<ActionResult<List<KanbanCommentDto>>> GetComments(int cardId)
    {
        var myId = await GetMyUserIdAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == cardId);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != myId) return Forbid();

        var comments = await db.KanbanComments
            .Where(c => c.CardId == cardId)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Author)
            .Select(c => new KanbanCommentDto(
                c.Id, c.CardId, c.AuthorId, c.Author.FullName, c.Author.ProfilePhotoUrl,
                c.Content, c.CreatedAt, c.UpdatedAt, c.IsEdited
            ))
            .ToListAsync();

        return Ok(comments);
    }

    /// <summary>
    /// Cria um comentário em um card e registra o evento no histórico.
    /// </summary>
    /// <param name="cardId">ID do card onde o comentário será criado.</param>
    /// <param name="req">`CreateCommentRequest` com o conteúdo do comentário.</param>
    /// <returns>O `KanbanCommentDto` criado com código HTTP 201.</returns>
    [HttpPost("cards/{cardId}/comments")]
    public async Task<ActionResult<KanbanCommentDto>> CreateComment(int cardId, [FromBody] CreateCommentRequest req)
    {
        var user = await GetMyUserAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == cardId);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != user.Id) return Forbid();

        var comment = new KanbanComment
        {
            CardId = cardId,
            AuthorId = user.Id,
            Content = req.Content
        };
        db.KanbanComments.Add(comment);

        db.KanbanCardHistories.Add(new KanbanCardHistory
        {
            CardId = cardId,
            UserId = user.Id,
            Action = KanbanHistoryAction.CommentAdded,
            Description = "adicionou um comentário"
        });

        await db.SaveChangesAsync();

        return Created($"/api/kanban/cards/{cardId}/comments/{comment.Id}", new KanbanCommentDto(
            comment.Id, comment.CardId, comment.AuthorId, user.FullName, user.ProfilePhotoUrl,
            comment.Content, comment.CreatedAt, comment.UpdatedAt, comment.IsEdited
        ));
    }

    /// <summary>
    /// Atualiza um comentário criado pelo próprio usuário.
    /// </summary>
    /// <param name="cardId">ID do card do comentário.</param>
    /// <param name="commentId">ID do comentário a ser atualizado.</param>
    /// <param name="req">`UpdateCommentRequest` com o novo conteúdo.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPut("cards/{cardId}/comments/{commentId}")]
    public async Task<IActionResult> UpdateComment(int cardId, int commentId, [FromBody] UpdateCommentRequest req)
    {
        var user = await GetMyUserAsync();
        var comment = await db.KanbanComments
            .Include(c => c.Card).ThenInclude(t => t.Column).ThenInclude(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.CardId == cardId);

        if (comment is null) return NotFound();
        if (comment.AuthorId != user.Id) return Forbid();

        comment.Content = req.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        comment.IsEdited = true;

        db.KanbanCardHistories.Add(new KanbanCardHistory
        {
            CardId = cardId,
            UserId = user.Id,
            Action = KanbanHistoryAction.CommentEdited,
            Description = "editou um comentário"
        });

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Remove um comentário (autor ou dono do quadro pode excluir).
    /// </summary>
    /// <param name="cardId">ID do card do comentário.</param>
    /// <param name="commentId">ID do comentário a ser removido.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpDelete("cards/{cardId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int cardId, int commentId)
    {
        var user = await GetMyUserAsync();
        var comment = await db.KanbanComments
            .Include(c => c.Card).ThenInclude(t => t.Column).ThenInclude(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.CardId == cardId);

        if (comment is null) return NotFound();
        if (comment.AuthorId != user.Id && comment.Card.Column.Board.OwnerId != user.Id) return Forbid();

        db.KanbanComments.Remove(comment);

        db.KanbanCardHistories.Add(new KanbanCardHistory
        {
            CardId = cardId,
            UserId = user.Id,
            Action = KanbanHistoryAction.CommentDeleted,
            Description = "removeu um comentário"
        });

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== History =====

    /// <summary>
    /// Retorna o histórico de ações de um card (movimentações, edições, comentários, etc.).
    /// </summary>
    /// <param name="cardId">ID do card cujo histórico será retornado.</param>
    /// <returns>Lista de `KanbanCardHistoryDto` ordenada por data decrescente.</returns>
    [HttpGet("cards/{cardId}/history")]
    public async Task<ActionResult<List<KanbanCardHistoryDto>>> GetHistory(int cardId)
    {
        var myId = await GetMyUserIdAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == cardId);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != myId) return Forbid();

        var history = await db.KanbanCardHistories
            .Where(h => h.CardId == cardId)
            .OrderByDescending(h => h.CreatedAt)
            .Include(h => h.User)
            .ToListAsync();

        var result = history.Select(h => new KanbanCardHistoryDto(
            h.Id, 
            h.CardId, 
            h.UserId, 
            h.User?.FullName ?? "Usuário",
            h.Action.ToString(), 
            h.Description ?? "", 
            h.OldValue, 
            h.NewValue, 
            h.CreatedAt
        )).ToList();

        return Ok(result);
    }

    // ===== Labels =====

    /// <summary>
    /// Obtém as labels (etiquetas) ativas de um quadro. Se `boardId` não for informado, usa o quadro padrão do usuário.
    /// </summary>
    /// <param name="boardId">ID opcional do quadro para filtrar as labels.</param>
    /// <returns>Lista de `KanbanLabelDto` ativas.</returns>
    [HttpGet("labels")]
    public async Task<ActionResult<List<KanbanLabelDto>>> GetLabels([FromQuery] int? boardId = null)
    {
        var myId = await GetMyUserIdAsync();
        
        KanbanBoard? board;
        if (boardId.HasValue)
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == boardId.Value && b.OwnerId == myId);
        }
        else
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        }
        
        if (board is null) return Ok(new List<KanbanLabelDto>());

        var labels = await db.KanbanLabels
            .Where(l => l.BoardId == board.Id && l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new KanbanLabelDto(l.Id, l.Name, l.Color))
            .ToListAsync();

        return Ok(labels);
    }

    /// <summary>
    /// Cria uma nova label (etiqueta) em um quadro.
    /// </summary>
    /// <param name="req">`CreateLabelRequest` com `BoardId` opcional, `Name` e `Color`.</param>
    /// <returns>O `KanbanLabelDto` criado com código HTTP 201.</returns>
    [HttpPost("labels")]
    public async Task<ActionResult<KanbanLabelDto>> CreateLabel([FromBody] CreateLabelRequest req)
    {
        var myId = await GetMyUserIdAsync();
        
        KanbanBoard? board;
        if (req.BoardId.HasValue)
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.Id == req.BoardId.Value && b.OwnerId == myId);
        }
        else
        {
            board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        }
        
        if (board is null) return NotFound("Crie o quadro primeiro");

        var label = new KanbanLabel
        {
            BoardId = board.Id,
            Name = req.Name,
            Color = req.Color
        };
        db.KanbanLabels.Add(label);
        await db.SaveChangesAsync();

        return Created($"/api/kanban/labels/{label.Id}", new KanbanLabelDto(label.Id, label.Name, label.Color));
    }

    /// <summary>
    /// Atualiza uma label existente (nome e cor).
    /// </summary>
    /// <param name="id">ID da label a ser atualizada.</param>
    /// <param name="req">`UpdateLabelRequest` com `Name` e `Color`.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPut("labels/{id}")]
    public async Task<IActionResult> UpdateLabel(int id, [FromBody] UpdateLabelRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var label = await db.KanbanLabels.Include(l => l.Board).FirstOrDefaultAsync(l => l.Id == id);
        if (label is null) return NotFound();
        if (label.Board.OwnerId != myId) return Forbid();

        label.Name = req.Name;
        label.Color = req.Color;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Marca uma label como inativa (soft-delete) para o quadro.
    /// </summary>
    /// <param name="id">ID da label a ser desativada.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpDelete("labels/{id}")]
    public async Task<IActionResult> DeleteLabel(int id)
    {
        var myId = await GetMyUserIdAsync();
        var label = await db.KanbanLabels.Include(l => l.Board).FirstOrDefaultAsync(l => l.Id == id);
        if (label is null) return NotFound();
        if (label.Board.OwnerId != myId) return Forbid();

        label.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Users for assignment =====

    /// <summary>
    /// Lista usuários que podem ser atribuídos a cards (usuários ativos, limitados a 100 resultados).
    /// </summary>
    /// <returns>Lista de objetos com `Id`, `FullName` e `Photo`.</returns>
    [HttpGet("users")]
    [HttpGet("assignable-users")]
    public async Task<ActionResult<List<object>>> GetAssignableUsers()
    {
        var usersList = await users.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, Photo = u.ProfilePhotoUrl })
            .Take(100)
            .ToListAsync();

        return Ok(usersList);
    }

    // ===== Columns =====

    /// <summary>
    /// Cria uma nova coluna no quadro do usuário e retorna a coluna criada.
    /// </summary>
    /// <param name="req">`CreateColumnRequest` com o título da coluna.</param>
    /// <returns>O `KanbanColumnDto` criado com código HTTP 201.</returns>
    [HttpPost("columns")]
    public async Task<ActionResult<KanbanColumnDto>> CreateColumn([FromBody] CreateColumnRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        if (board is null) return NotFound("Crie o quadro primeiro");
        var max = await db.KanbanColumns.Where(c => c.BoardId == board.Id).MaxAsync(c => (int?)c.Position) ?? -1;
        var col = new KanbanColumn { BoardId = board.Id, Title = req.Title, Position = max + 1 };
        db.KanbanColumns.Add(col);
        await db.SaveChangesAsync();
        return Created($"/api/kanban/columns/{col.Id}", new KanbanColumnDto(col.Id, col.Title, col.Position));
    }

    /// <summary>
    /// Renomeia uma coluna do quadro do usuário.
    /// </summary>
    /// <param name="id">ID da coluna a ser renomeada.</param>
    /// <param name="req">`RenameColumnRequest` com o novo título.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPut("columns/{id}/title")]
    public async Task<IActionResult> RenameColumn(int id, [FromBody] RenameColumnRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var col = await db.KanbanColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == id);
        if (col is null) return NotFound();
        if (col.Board.OwnerId != myId) return Forbid();
        col.Title = req.Title;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Exclui uma coluna (apenas se não houver cards na coluna) e ajusta posições.
    /// </summary>
    /// <param name="id">ID da coluna a ser excluída.</param>
    /// <returns>204 No Content em sucesso, 400 se a coluna tiver cards, 404/403 conforme aplicável.</returns>
    [HttpDelete("columns/{id}")]
    public async Task<IActionResult> DeleteColumn(int id)
    {
        var myId = await GetMyUserIdAsync();
        var col = await db.KanbanColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == id);
        if (col is null) return NotFound();
        if (col.Board.OwnerId != myId) return Forbid();

        // Check if column has cards
        var hasCards = await db.KanbanCards.AnyAsync(c => c.ColumnId == id);
        if (hasCards)
        {
            return BadRequest("Não é possível excluir uma coluna com cards. Mova ou exclua os cards primeiro.");
        }

        // Shift positions of columns after this one
        var laterCols = await db.KanbanColumns
            .Where(c => c.BoardId == col.BoardId && c.Position > col.Position)
            .ToListAsync();
        foreach (var c in laterCols) c.Position--;

        db.KanbanColumns.Remove(col);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Reordena as colunas no quadro (ajusta posições entre colunas existentes).
    /// </summary>
    /// <param name="req">`ReorderColumnRequest` com `ColumnId` e `NewPosition`.</param>
    /// <returns>204 No Content em sucesso, 404/403 conforme aplicável.</returns>
    [HttpPost("columns/reorder")]
    public async Task<IActionResult> ReorderColumn([FromBody] ReorderColumnRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var col = await db.KanbanColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == req.ColumnId);
        if (col is null) return NotFound();
        if (col.Board.OwnerId != myId) return Forbid();

        var cols = await db.KanbanColumns.Where(c => c.BoardId == col.BoardId).OrderBy(c => c.Position).ToListAsync();
        var oldPos = col.Position;
        var newPos = Math.Clamp(req.NewPosition, 0, cols.Count - 1);
        if (newPos == oldPos) return NoContent();

        if (newPos > oldPos)
        {
            foreach (var c in cols.Where(c => c.Position > oldPos && c.Position <= newPos)) c.Position--;
        }
        else
        {
            foreach (var c in cols.Where(c => c.Position < oldPos && c.Position >= newPos)) c.Position++;
        }
        col.Position = newPos;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
