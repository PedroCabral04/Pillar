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
    private async Task<int> GetMyUserIdAsync()
    {
        var u = await users.GetUserAsync(User);
        if (u is null) throw new UnauthorizedAccessException();
        return u.Id;
    }

    [HttpGet("board")] // get or create personal board
    public async Task<ActionResult<KanbanBoardDto>> GetOrCreateMyBoard()
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        if (board is null)
        {
            board = new KanbanBoard { OwnerId = myId, Name = "Meu quadro" };
            db.KanbanBoards.Add(board);
            await db.SaveChangesAsync();

            // default columns
            db.KanbanColumns.AddRange(
                new KanbanColumn { BoardId = board.Id, Title = "A Fazer", Position = 0 },
                new KanbanColumn { BoardId = board.Id, Title = "Fazendo", Position = 1 },
                new KanbanColumn { BoardId = board.Id, Title = "Feito", Position = 2 }
            );
            await db.SaveChangesAsync();
        }
        return Ok(new KanbanBoardDto(board.Id, board.Name));
    }

    [HttpGet("columns")] // list columns + cards
    public async Task<ActionResult<object>> GetColumns()
    {
        var myId = await GetMyUserIdAsync();
        var board = await db.KanbanBoards.FirstOrDefaultAsync(b => b.OwnerId == myId);
        if (board is null) return Ok(new { columns = new object[0] });

        var cols = await db.KanbanColumns
            .Where(c => c.BoardId == board.Id)
            .OrderBy(c => c.Position)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Position,
                cards = db.KanbanCards.Where(t => t.ColumnId == c.Id).OrderBy(t => t.Position)
                    .Select(t => new KanbanCardDto(t.Id, t.Title, t.Description, t.Position, t.ColumnId)).ToList()
            })
            .ToListAsync();

        return Ok(new { columns = cols });
    }

    [HttpPost("cards")] // create card
    public async Task<ActionResult<KanbanCardDto>> CreateCard([FromBody] CreateCardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var column = await db.KanbanColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == req.ColumnId);
        if (column is null) return NotFound("Coluna não encontrada");
        var board = column.Board;
        if (board.OwnerId != myId) return Forbid();

        var maxPos = await db.KanbanCards.Where(t => t.ColumnId == column.Id).MaxAsync(t => (int?)t.Position) ?? -1;
        var card = new KanbanCard
        {
            ColumnId = column.Id,
            Title = req.Title,
            Description = req.Description,
            Position = maxPos + 1
        };
        db.KanbanCards.Add(card);
        await db.SaveChangesAsync();
        return Created($"/api/kanban/cards/{card.Id}", new KanbanCardDto(card.Id, card.Title, card.Description, card.Position, card.ColumnId));
    }

    [HttpPost("cards/move")] // move or reorder card
    public async Task<IActionResult> MoveCard([FromBody] MoveCardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var card = await db.KanbanCards.Include(t => t.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == req.CardId);
        if (card is null) return NotFound();
        if (card.Column.Board.OwnerId != myId) return Forbid();

        // normalize target position
        var targetCards = await db.KanbanCards.Where(t => t.ColumnId == req.ToColumnId).OrderBy(t => t.Position).ToListAsync();
        var newPos = Math.Clamp(req.ToPosition, 0, targetCards.Count);

        // if moving across columns, shift positions in old column
        if (card.ColumnId != req.ToColumnId)
        {
            var oldColumnCards = await db.KanbanCards.Where(t => t.ColumnId == card.ColumnId && t.Position > card.Position).ToListAsync();
            foreach (var c in oldColumnCards) c.Position--;
            card.ColumnId = req.ToColumnId;
            await db.SaveChangesAsync();
            targetCards = await db.KanbanCards.Where(t => t.ColumnId == req.ToColumnId).OrderBy(t => t.Position).ToListAsync();
        }

        // insert at new position, shift others
        foreach (var t in targetCards.Where(t => t.Position >= newPos)) t.Position++;
        card.Position = newPos;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("columns")] // create column at end
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

    [HttpPut("columns/{id}/title")] // rename column
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

    [HttpPost("columns/reorder")] // reorder column positions
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
