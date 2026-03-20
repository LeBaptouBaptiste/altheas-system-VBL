using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API_Althea_systems.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (token != null)
        {
            AttachUserToContext(context, token);
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader["Bearer ".Length..].Trim();
    }

    private void AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                // Attach claims to HttpContext for use in controllers/services
                context.Items["UserId"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                context.Items["UserEmail"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                context.Items["UserRole"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            }
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogDebug("JWT token expired");
            // Token expired — don't attach user, let [Authorize] handle 401
        }
        catch (Exception ex)
        {
            _logger.LogDebug("JWT validation failed: {Message}", ex.Message);
            // Invalid token — don't attach user, let [Authorize] handle 401
        }
    }
}
