namespace erp.Security;

/// <summary>
/// Middleware para adicionar headers de segurança HTTP em todas as respostas
/// Protege contra XSS, clickjacking, MIME sniffing e outros vetores de ataque
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _next = next;
        _configuration = configuration;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Previne MIME sniffing pelo navegador
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Protege contra clickjacking (DENY previne todos os frames)
        // Em CSP mode pode ser removido, mas mantido para compatibilidade com browsers antigos
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Referrer-Policy: Controla informações enviadas via Referer header
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy (antigo Feature-Policy): Controla quais APIs/features o navegador pode usar
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "speaker=(), " +
            "fullscreen=(self), " +
            "payment=()");

        // Content-Security-Policy (CSP): Previne XSS controlando de onde o conteúdo pode ser carregado
        var cspBuilder = BuildCspPolicy(context);
        context.Response.Headers.Append("Content-Security-Policy", cspBuilder);

        // X-XSS-Protection: Ativa filtro XSS do navegador (obsoleto mas mantido para compatibilidade)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Strict-Transport-Security: Força HTTPS em futuras requisições (apenas em produção com HTTPS)
        if (!_env.IsDevelopment() && context.Request.IsHttps)
        {
            var hstsMaxAge = _configuration.GetValue<int>("Security:HstsMaxAge", 365); // dias
            context.Response.Headers.Append("Strict-Transport-Security",
                $"max-age={hstsMaxAge * 86400}; includeSubDomains; preload");
        }

        await _next(context);
    }

    private string BuildCspPolicy(HttpContext context)
    {
        // Configuração base da CSP - pode ser customizada via appsettings.json se necessário
        var useReportOnly = _configuration.GetValue<bool>("Security:CspReportOnly", false);
        var reportUri = _configuration.GetValue<string>("Security:CspReportUri");

        // Política CSP para Blazor Server
        // Blazor Server usa SignalR, então precisamos permitir websockets e scripts inline
        var policy = new List<string>
        {
            "default-src 'self'",                           // Apenas mesma origem por default
            "base-uri 'self'",                              // Restringe URLs base
            "form-action 'self'",                           // Restringe form submissions
            "frame-ancestors 'none'",                       // Previne clickjacking
            "img-src 'self' data: https: blob:",            // Imagens
            "font-src 'self' data:",                        // Fontes
            "style-src 'self' 'unsafe-inline'",             // CSS (unsafe-inline necessário para Blazor)
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // JS (necessário para Blazor Server)
            "connect-src 'self' wss: ws:",                  // SignalR/websockets
            "object-src 'none'",                            // Bloqueia plugins
            "media-src 'self'",
            "manifest-src 'self'"
        };

        // Adicionar report-uri se configurado (para monitoramento de violações CSP)
        if (!string.IsNullOrEmpty(reportUri))
        {
            policy.Add($"report-uri {reportUri}");
        }

        return string.Join("; ", policy);
    }
}

/// <summary>
/// Extension method para registrar o middleware de forma fluent
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
