using System.Net;
using System.Text.Json;
using API_Althea_systems.Common.Exceptions;

namespace API_Althea_systems.Middleware;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var mapping = exception switch
        {
            NotFoundException ex => new ErrorMapping(HttpStatusCode.NotFound, ex.Message),
            AppValidationException ex => new ErrorMapping(HttpStatusCode.BadRequest, ex.Message, Errors: ex.Errors),
            UnauthorizedException ex => new ErrorMapping(HttpStatusCode.Unauthorized, ex.Message, Reason: ex.Reason),
            ForbiddenException ex => new ErrorMapping(HttpStatusCode.Forbidden, ex.Message),
            ConflictException ex => new ErrorMapping(HttpStatusCode.Conflict, ex.Message),
            AccountLockedException ex => new ErrorMapping(
                (HttpStatusCode)429,
                ex.Message,
                Reason: "account_locked",
                RetryAfter: ex.RetryAfterSeconds),
            _ => new ErrorMapping(HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        // Log based on severity
        if (mapping.StatusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}", (int)mapping.StatusCode, exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)mapping.StatusCode;

        // Standard HTTP semantics: tell the client how long to wait before retrying.
        if (mapping.RetryAfter is { } retryAfter)
        {
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
        }

        var response = new ErrorResponse
        {
            Success = false,
            StatusCode = (int)mapping.StatusCode,
            Message = mapping.Message,
            Errors = mapping.Errors,
            Reason = mapping.Reason,
            RetryAfterSeconds = mapping.RetryAfter,
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });

        await context.Response.WriteAsync(json);
    }

    private record ErrorMapping(
        HttpStatusCode StatusCode,
        string Message,
        IDictionary<string, string[]>? Errors = null,
        string? Reason = null,
        int? RetryAfter = null);
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
    public string? Reason { get; set; }
    public int? RetryAfterSeconds { get; set; }
}
