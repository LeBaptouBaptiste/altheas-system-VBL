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
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException ex => (
                HttpStatusCode.NotFound,
                ex.Message,
                (IDictionary<string, string[]>?)null
            ),
            AppValidationException ex => (
                HttpStatusCode.BadRequest,
                ex.Message,
                ex.Errors
            ),
            UnauthorizedException ex => (
                HttpStatusCode.Unauthorized,
                ex.Message,
                (IDictionary<string, string[]>?)null
            ),
            ForbiddenException ex => (
                HttpStatusCode.Forbidden,
                ex.Message,
                (IDictionary<string, string[]>?)null
            ),
            ConflictException ex => (
                HttpStatusCode.Conflict,
                ex.Message,
                (IDictionary<string, string[]>?)null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (IDictionary<string, string[]>?)null
            )
        };

        // Log based on severity
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}", (int)statusCode, exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            Success = false,
            StatusCode = (int)statusCode,
            Message = message,
            Errors = errors
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
}
