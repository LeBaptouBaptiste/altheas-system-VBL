using System.Diagnostics;
using System.Text;

namespace API_Althea_systems.Middleware;

public class DebugHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DebugHandlerMiddleware> _logger;

    public DebugHandlerMiddleware(RequestDelegate next, ILogger<DebugHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // ── Log request ──────────────────────────────────
        var request = context.Request;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var requestLog = new StringBuilder();
        requestLog.AppendLine($"╔══ REQUEST [{requestId}] ══════════════════════════");
        requestLog.AppendLine($"║ {request.Method} {request.Path}{request.QueryString}");
        requestLog.AppendLine($"║ Client IP: {clientIp}");
        requestLog.AppendLine($"║ Content-Type: {request.ContentType ?? "N/A"}");
        requestLog.AppendLine($"║ User-Agent: {request.Headers.UserAgent}");

        // Log auth header presence (not the value)
        if (request.Headers.ContainsKey("Authorization"))
            requestLog.AppendLine($"║ Authorization: Bearer ***");

        // Log body for POST/PUT/PATCH (truncated)
        if (HttpMethods.IsPost(request.Method) ||
            HttpMethods.IsPut(request.Method) ||
            HttpMethods.IsPatch(request.Method))
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            if (!string.IsNullOrEmpty(body))
            {
                var truncated = body.Length > 500 ? body[..500] + "... [truncated]" : body;
                requestLog.AppendLine($"║ Body: {truncated}");
            }
        }

        requestLog.Append($"╚═════════════════════════════════════════════════");
        _logger.LogInformation("{RequestLog}", requestLog.ToString());

        // ── Execute pipeline ─────────────────────────────
        await _next(context);

        // ── Log response ─────────────────────────────────
        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;
        var statusCode = context.Response.StatusCode;

        var responseLog = new StringBuilder();
        responseLog.AppendLine($"╔══ RESPONSE [{requestId}] ═════════════════════════");
        responseLog.AppendLine($"║ Status: {statusCode}");
        responseLog.AppendLine($"║ Duration: {elapsed}ms");
        responseLog.AppendLine($"║ Content-Type: {context.Response.ContentType ?? "N/A"}");
        responseLog.Append($"╚═════════════════════════════════════════════════");

        if (statusCode >= 400)
            _logger.LogWarning("{ResponseLog}", responseLog.ToString());
        else
            _logger.LogInformation("{ResponseLog}", responseLog.ToString());
    }
}
