using erp.Data;
using erp.Models.Audit;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;

namespace erp.Security;

/// <summary>
/// Filtro de ação que intercepta métodos marcados com [AuditRead]
/// e registra o acesso a dados sensíveis para compliance LGPD/GDPR
/// </summary>
public class AuditReadActionFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditReadActionFilter> _logger;

    public AuditReadActionFilter(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditReadActionFilter> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Busca o atributo [AuditRead] no método ou classe
        var auditReadAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<AuditReadAttribute>()
            .FirstOrDefault();

        // Se não tem o atributo, apenas executa a ação normalmente
        if (auditReadAttribute == null)
        {
            await next();
            return;
        }

        // Previne duplicação: verifica se já registramos este request
        var requestKey = $"AuditRead_{context.HttpContext.TraceIdentifier}";
        if (context.HttpContext.Items.ContainsKey(requestKey))
        {
            await next();
            return;
        }
        
        // Marca que já processamos este request
        context.HttpContext.Items[requestKey] = true;

        // Executa a ação
        var executedContext = await next();

        // Se a ação foi bem-sucedida (200 OK), registra o acesso
        if (executedContext.Exception == null && executedContext.HttpContext.Response.StatusCode == 200)
        {
            try
            {
                await LogReadAccessAsync(context, auditReadAttribute);
            }
            catch (Exception ex)
            {
                // Não devemos falhar a requisição por erro de auditoria
                _logger.LogError(ex, "Erro ao registrar auditoria de leitura");
            }
        }
    }

    private async Task LogReadAccessAsync(ActionExecutingContext context, AuditReadAttribute attribute)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Extrai informações do usuário
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = httpContext.User.Identity?.Name ?? "Anonymous";

        // Extrai IP e User Agent
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        // Extrai EntityId dos parâmetros de rota ou query string
        var entityId = ExtractEntityId(context);

        // Captura parâmetros se configurado
        string? parameters = null;
        if (attribute.IncludeParameters && context.ActionArguments.Any())
        {
            try
            {
                // Remove dados sensíveis como passwords antes de serializar
                var sanitizedParams = context.ActionArguments
                    .Where(p => !p.Key.ToLower().Contains("password") && 
                                !p.Key.ToLower().Contains("token"))
                    .ToDictionary(k => k.Key, v => v.Value);
                
                parameters = JsonSerializer.Serialize(sanitizedParams);
            }
            catch
            {
                parameters = "Error serializing parameters";
            }
        }

        // Cria o log de auditoria
        var auditLog = new AuditLog
        {
            EntityName = attribute.EntityName,
            EntityId = entityId ?? "N/A",
            Action = AuditAction.Read,
            UserId = int.TryParse(userId, out var uid) ? uid : null,
            UserName = userName,
            IpAddress = ipAddress.Length > 45 ? ipAddress.Substring(0, 45) : ipAddress,
            UserAgent = userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
            Timestamp = DateTime.UtcNow,
            AdditionalInfo = BuildAdditionalInfo(attribute, parameters)
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    private string? ExtractEntityId(ActionExecutingContext context)
    {
        // Tenta encontrar um parâmetro chamado "id" ou que termine com "Id"
        var idParam = context.ActionArguments
            .FirstOrDefault(p => p.Key.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                                 p.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

        return idParam.Value?.ToString();
    }

    private string BuildAdditionalInfo(AuditReadAttribute attribute, string? parameters)
    {
        var info = new Dictionary<string, object?>
        {
            { "Sensitivity", attribute.Sensitivity.ToString() },
            { "Description", attribute.Description }
        };

        if (!string.IsNullOrEmpty(parameters))
        {
            info["Parameters"] = parameters;
        }

        return JsonSerializer.Serialize(info);
    }
}
