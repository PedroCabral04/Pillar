using System.Net;
using System.Text.Json;
using erp.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace erp.Security;

/// <summary>
/// Global exception handler that provides generic error messages to clients
/// while logging detailed errors server-side. Prevents information disclosure.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the full exception server-side
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        // Determine status code based on exception type
        var (statusCode, message, errorCode) = GetExceptionInfo(exception);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Include validation errors if applicable
        IDictionary<string, string[]>? validationErrors = null;
        if (exception is ValidationException validationEx && validationEx.Errors.Count > 0)
        {
            validationErrors = validationEx.Errors;
        }

        // Build response
        var response = new ErrorResponse
        {
            Status = (int)statusCode,
            Message = message,
            ErrorCode = errorCode,
            // Only include stack trace in development
            StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
            ValidationErrors = validationErrors
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Returns HTTP status code, message, and error code based on exception type.
    /// </summary>
    private static (HttpStatusCode StatusCode, string Message, string? ErrorCode) GetExceptionInfo(Exception ex)
    {
        return ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message, ((NotFoundException)ex).ErrorCode),
            BusinessRuleException => (HttpStatusCode.BadRequest, ex.Message, ((BusinessRuleException)ex).ErrorCode),
            ValidationException => (HttpStatusCode.BadRequest, ex.Message, ((ValidationException)ex).ErrorCode),
            UnauthorizedException => (HttpStatusCode.Forbidden, ex.Message, ((UnauthorizedException)ex).ErrorCode),
            ConflictException => (HttpStatusCode.Conflict, ex.Message, ((ConflictException)ex).ErrorCode),
            ArgumentNullException or ArgumentException => (HttpStatusCode.BadRequest, "A solicitação contém parâmetros inválidos.", "INVALID_ARGUMENT"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Você não tem permissão para acessar este recurso.", "UNAUTHORIZED"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "O recurso solicitado não foi encontrado.", "NOT_FOUND"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "A operação não é válida no estado atual.", "INVALID_OPERATION"),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno no servidor. Tente novamente mais tarde.", "INTERNAL_ERROR")
        };
    }

    private record ErrorResponse
    {
        public int Status { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? ErrorCode { get; init; }
        public string? StackTrace { get; init; }
        public IDictionary<string, string[]>? ValidationErrors { get; init; }
    }
}
