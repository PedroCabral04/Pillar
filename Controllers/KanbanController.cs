using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Kanban;
using erp.Models.Identity;
using erp.Services.Kanban;
using erp.Services.Tenancy;

namespace erp.Controllers;

[ApiController]
[Route("api/kanban")]
[Authorize]
public class KanbanController(
    IKanbanService kanbanService,
    UserManager<ApplicationUser> users,
    ITenantContextAccessor tenantContextAccessor) : ControllerBase
{
    private async Task<int> GetMyUserIdAsync()
    {
        var u = await users.GetUserAsync(User);
        if (u is null) throw new UnauthorizedAccessException();
        return u.Id;
    }

    private int GetRequiredTenantId()
    {
        return tenantContextAccessor.Current?.TenantId ?? throw new InvalidOperationException("Tenant context is required");
    }

    // ===== Boards (Multiple) =====

    [HttpGet("boards")]
    public async Task<ActionResult<List<KanbanBoardDto>>> GetMyBoards()
    {
        var myId = await GetMyUserIdAsync();
        var boards = await kanbanService.GetMyBoardsAsync(myId);
        return Ok(boards);
    }

    [HttpPost("boards")]
    public async Task<ActionResult<KanbanBoardDto>> CreateBoard([FromBody] CreateBoardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var tenantId = GetRequiredTenantId();
        var board = await kanbanService.CreateBoardAsync(req, myId, tenantId);
        return Created($"/api/kanban/boards/{board.Id}", board);
    }

    [HttpGet("boards/{id}")]
    public async Task<ActionResult<KanbanBoardDto>> GetBoard(int id)
    {
        var myId = await GetMyUserIdAsync();
        var board = await kanbanService.GetBoardAsync(id, myId);
        if (board == null) return NotFound();
        return Ok(board);
    }

    [HttpPut("boards/{id}/name")]
    public async Task<IActionResult> RenameBoard(int id, [FromBody] RenameBoardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.RenameBoardAsync(id, req.Name, myId);
        return NoContent();
    }

    [HttpDelete("boards/{id}")]
    public async Task<IActionResult> DeleteBoard(int id)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.DeleteBoardAsync(id, myId);
        return NoContent();
    }

    // ===== Board (Legacy - get or create default) =====

    [HttpGet("board")]
    public async Task<ActionResult<KanbanBoardDto>> GetOrCreateMyBoard()
    {
        var myId = await GetMyUserIdAsync();
        var tenantId = GetRequiredTenantId();
        var board = await kanbanService.GetOrCreateMyBoardAsync(myId, tenantId);
        return Ok(board);
    }

    // ===== Columns =====

    [HttpGet("columns")]
    public async Task<ActionResult<object>> GetColumns([FromQuery] int? boardId = null)
    {
        var myId = await GetMyUserIdAsync();
        var columns = await kanbanService.GetColumnsAsync(boardId, myId);
        return Ok(new { columns });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<KanbanStatsDto>> GetStats([FromQuery] int? boardId = null)
    {
        var myId = await GetMyUserIdAsync();
        var stats = await kanbanService.GetStatsAsync(boardId, myId);
        return Ok(stats);
    }

    // ===== Cards =====

    [HttpGet("cards/{id}")]
    public async Task<ActionResult<KanbanCardDto>> GetCard(int id)
    {
        var myId = await GetMyUserIdAsync();
        var card = await kanbanService.GetCardAsync(id, myId);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [HttpPost("cards")]
    public async Task<ActionResult<KanbanCardDto>> CreateCard([FromBody] CreateCardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var tenantId = GetRequiredTenantId();
        var card = await kanbanService.CreateCardAsync(req, myId, tenantId);
        return Created($"/api/kanban/cards/{card.Id}", card);
    }

    [HttpPut("cards/{id}")]
    public async Task<IActionResult> UpdateCard(int id, [FromBody] UpdateCardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.UpdateCardAsync(id, req, myId);
        return NoContent();
    }

    [HttpDelete("cards/{id}")]
    public async Task<IActionResult> DeleteCard(int id)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.DeleteCardAsync(id, myId);
        return NoContent();
    }

    [HttpPost("cards/{id}/archive")]
    public async Task<IActionResult> ArchiveCard(int id, [FromBody] ArchiveCardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.ArchiveCardAsync(id, req.IsArchived, myId);
        return NoContent();
    }

    [HttpPost("cards/move")]
    public async Task<IActionResult> MoveCard([FromBody] MoveCardRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.MoveCardAsync(req, myId);
        return NoContent();
    }

    // ===== Comments =====

    [HttpGet("cards/{cardId}/comments")]
    public async Task<ActionResult<List<KanbanCommentDto>>> GetComments(int cardId)
    {
        var myId = await GetMyUserIdAsync();
        var comments = await kanbanService.GetCommentsAsync(cardId, myId);
        return Ok(comments);
    }

    [HttpPost("cards/{cardId}/comments")]
    public async Task<ActionResult<KanbanCommentDto>> CreateComment(int cardId, [FromBody] CreateCommentRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var comment = await kanbanService.CreateCommentAsync(cardId, req, myId);
        return Created($"/api/kanban/cards/{cardId}/comments/{comment.Id}", comment);
    }

    [HttpPut("cards/{cardId}/comments/{commentId}")]
    public async Task<IActionResult> UpdateComment(int cardId, int commentId, [FromBody] UpdateCommentRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.UpdateCommentAsync(cardId, commentId, req, myId);
        return NoContent();
    }

    [HttpDelete("cards/{cardId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int cardId, int commentId)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.DeleteCommentAsync(cardId, commentId, myId);
        return NoContent();
    }

    // ===== History =====

    [HttpGet("cards/{cardId}/history")]
    public async Task<ActionResult<List<KanbanCardHistoryDto>>> GetHistory(int cardId)
    {
        var myId = await GetMyUserIdAsync();
        var history = await kanbanService.GetHistoryAsync(cardId, myId);
        return Ok(history);
    }

    // ===== Labels =====

    [HttpGet("labels")]
    public async Task<ActionResult<List<KanbanLabelDto>>> GetLabels([FromQuery] int? boardId = null)
    {
        var myId = await GetMyUserIdAsync();
        var labels = await kanbanService.GetLabelsAsync(boardId, myId);
        return Ok(labels);
    }

    [HttpPost("labels")]
    public async Task<ActionResult<KanbanLabelDto>> CreateLabel([FromBody] CreateLabelRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var label = await kanbanService.CreateLabelAsync(req, myId);
        return Created($"/api/kanban/labels/{label.Id}", label);
    }

    [HttpPut("labels/{id}")]
    public async Task<IActionResult> UpdateLabel(int id, [FromBody] UpdateLabelRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.UpdateLabelAsync(id, req, myId);
        return NoContent();
    }

    [HttpDelete("labels/{id}")]
    public async Task<IActionResult> DeleteLabel(int id)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.DeleteLabelAsync(id, myId);
        return NoContent();
    }

    // ===== Users for assignment =====

    [HttpGet("users")]
    [HttpGet("assignable-users")]
    public async Task<ActionResult<List<object>>> GetAssignableUsers()
    {
        var users = await kanbanService.GetAssignableUsersAsync();
        return Ok(users);
    }

    // ===== Columns management =====

    [HttpPost("columns")]
    public async Task<ActionResult<KanbanColumnDto>> CreateColumn([FromBody] CreateColumnRequest req)
    {
        var myId = await GetMyUserIdAsync();
        var tenantId = GetRequiredTenantId();
        var column = await kanbanService.CreateColumnAsync(req, myId, tenantId);
        return Created($"/api/kanban/columns/{column.Id}", column);
    }

    [HttpPut("columns/{id}/title")]
    public async Task<IActionResult> RenameColumn(int id, [FromBody] RenameColumnRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.RenameColumnAsync(id, req.Title, myId);
        return NoContent();
    }

    [HttpDelete("columns/{id}")]
    public async Task<IActionResult> DeleteColumn(int id)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.DeleteColumnAsync(id, myId);
        return NoContent();
    }

    [HttpPost("columns/reorder")]
    public async Task<IActionResult> ReorderColumn([FromBody] ReorderColumnRequest req)
    {
        var myId = await GetMyUserIdAsync();
        await kanbanService.ReorderColumnAsync(req.ColumnId, req.NewPosition, myId);
        return NoContent();
    }
}
