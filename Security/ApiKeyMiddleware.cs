using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace erp.Security;

public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _apiKey = config["Security:ApiKey"]; // null => bypass enforcement for now
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce when configured and for API routes
        if (!string.IsNullOrEmpty(_apiKey) && context.Request.Path.StartsWithSegments("/api"))
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out StringValues provided) || provided.Count == 0 || provided[0] != _apiKey)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Missing or invalid API key");
                return;
            }
        }

        await _next(context);
    }
}
