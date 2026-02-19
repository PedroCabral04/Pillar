using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Notifications;
using erp.Security;
using erp.DTOs.Notifications;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/notificacoes")]
[Authorize]
/// <summary>
/// Controlador responsável por operações relacionadas a notificações.
/// Exponha endpoints para listar, criar, marcar como lido, excluir e gerenciar preferências.
/// </summary>
public class NotificationsController : ControllerBase
{
    private readonly IAdvancedNotificationService _notificationService;

    public NotificationsController(IAdvancedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Recupera a lista de notificações do usuário autenticado, aplicando filtros opcionais.
    /// </summary>
    /// <param name="filter">Filtros opcionais para paginação e seleção de notificações.</param>
    /// <returns>Lista de notificações do usuário.</returns>
    [HttpGet]
    public async Task<ActionResult<List<Notification>>> GetNotifications([FromQuery] NotificationFilter filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, filter);
        return Ok(notifications);
    }

    /// <summary>
    /// Recupera uma única notificação pelo seu identificador.
    /// Garante que o usuário autenticado seja o proprietário da notificação.
    /// </summary>
    /// <param name="id">Identificador da notificação.</param>
    /// <returns>A notificação solicitada ou um código de erro apropriado.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> GetNotification(string id)
    {
        var notification = await _notificationService.GetNotificationAsync(id);
        if (notification == null)
        {
            return NotFound(new { error = "Notification not found" });
        }

        // Verify ownership
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (notification.UserId != userId)
        {
            return Forbid();
        }

        return Ok(notification);
    }

    /// <summary>
    /// Retorna um resumo das notificações do usuário (total, não lidas, urgentes, etc.).
    /// </summary>
    /// <returns>Resumo das notificações do usuário.</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<NotificationSummary>> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var summary = await _notificationService.GetSummaryAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Cria uma nova notificação. Apenas usuários administradores podem usar este endpoint.
    /// </summary>
    /// <param name="request">Dados para criação da notificação.</param>
    /// <returns>A notificação criada com código 201 e localização.</returns>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminTenantOrSuperAdmin)]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var notification = await _notificationService.CreateNotificationAsync(request);
        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
    }

    /// <summary>
    /// Cria múltiplas notificações em lote. Requer permissão de administrador.
    /// </summary>
    /// <param name="request">Dados do lote de notificações a serem enviadas.</param>
    /// <returns>Lista de notificações criadas.</returns>
    [HttpPost("bulk")]
    [Authorize(Roles = RoleNames.AdminTenantOrSuperAdmin)]
    public async Task<ActionResult<List<Notification>>> CreateBulkNotifications([FromBody] BulkNotificationRequest request)
    {
        var notifications = await _notificationService.CreateBulkNotificationsAsync(request);
        return Ok(notifications);
    }

    /// <summary>
    /// Marca uma notificação específica como lida pelo usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da notificação a ser marcada como lida.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPatch("{id}/read")]
    public async Task<ActionResult> MarkAsRead(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(userId, id);
        if (!success)
        {
            return NotFound(new { error = "Notification not found" });
        }

        return Ok(new { message = "Notification marked as read" });
    }

    /// <summary>
    /// Marca todas as notificações do usuário autenticado como lidas.
    /// </summary>
    /// <returns>Quantidade de notificações marcadas como lidas.</returns>
    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { message = $"{count} notifications marked as read" });
    }

    /// <summary>
    /// Marca múltiplas notificações como lidas. Pode marcar todas quando `MarkAll=true`.
    /// </summary>
    /// <param name="request">Identificadores das notificações ou flag para marcar todas.</param>
    /// <returns>Resultado com a quantidade marcada.</returns>
    [HttpPost("mark-multiple-read")]
    public async Task<ActionResult> MarkMultipleAsRead([FromBody] MarkAsReadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.MarkAll)
        {
            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = $"{count} notifications marked as read" });
        }

        if (request.NotificationIds == null || !request.NotificationIds.Any())
        {
            return BadRequest(new { error = "NotificationIds required when MarkAll is false" });
        }

        var markedCount = await _notificationService.MarkMultipleAsReadAsync(userId, request.NotificationIds);
        return Ok(new { message = $"{markedCount} notifications marked as read" });
    }

    /// <summary>
    /// Exclui uma notificação do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da notificação a ser excluída.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.DeleteNotificationAsync(userId, id);
        if (!success)
        {
            return NotFound(new { error = "Notification not found" });
        }

        return Ok(new { message = "Notification deleted successfully" });
    }

    /// <summary>
    /// Exclui todas as notificações que já estão marcadas como lidas do usuário.
    /// </summary>
    /// <returns>Quantidade de notificações lidas excluídas.</returns>
    [HttpDelete("read/all")]
    public async Task<ActionResult> DeleteAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.DeleteAllReadAsync(userId);
        return Ok(new { message = $"{count} read notifications deleted" });
    }

    /// <summary>
    /// Recupera as preferências de notificação do usuário autenticado.
    /// </summary>
    /// <returns>Objeto `NotificationPreferences` do usuário.</returns>
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferences>> GetPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var preferences = await _notificationService.GetPreferencesAsync(userId);
        return Ok(preferences);
    }

    /// <summary>
    /// Salva ou atualiza as preferências de notificação do usuário autenticado.
    /// </summary>
    /// <param name="preferences">Objeto com as preferências a serem salvas.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("preferences")]
    public async Task<ActionResult> SavePreferences([FromBody] NotificationPreferences preferences)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _notificationService.SavePreferencesAsync(userId, preferences);
        return Ok(new { message = "Preferences saved successfully" });
    }
}
