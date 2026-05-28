using API_Althea_systems.Common.Exceptions;

namespace API_Althea_systems.Common.Auth;

/// <summary>
/// Helpers around the per-request user context populated by
/// <see cref="API_Althea_systems.Middleware.JwtMiddleware"/>.
///
/// JwtMiddleware writes <c>UserId</c>, <c>UserRole</c>, and friends into
/// <c>HttpContext.Items</c> (only for tokens whose <c>purpose == "access"</c>).
/// Controllers used to repeat the same brittle <c>Guid.Parse(... ?.ToString()!)</c>
/// boilerplate; this extension centralizes the lookup AND adds a typed
/// ownership-check helper so that AuthZ rules read uniformly:
///
///   <code>http.RequireOwnershipOrAdmin(targetUserId);</code>
///
/// Throws <see cref="ForbiddenException"/> when the caller is neither the
/// resource owner nor an Admin — mapped by ErrorHandlerMiddleware to 403.
/// </summary>
public static class HttpContextExtensions
{
    public static Guid? GetCurrentUserId(this HttpContext context)
    {
        var raw = context.Items["UserId"]?.ToString();
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static string? GetCurrentUserRole(this HttpContext context)
        => context.Items["UserRole"]?.ToString();

    public static bool IsCurrentUserAdmin(this HttpContext context)
        => string.Equals(context.GetCurrentUserRole(), "Admin", StringComparison.Ordinal);

    /// <summary>
    /// Enforces that the authenticated caller either owns
    /// <paramref name="targetUserId"/> or has the Admin role.
    /// Throws <see cref="ForbiddenException"/> otherwise.
    /// </summary>
    public static void RequireOwnershipOrAdmin(this HttpContext context, Guid targetUserId)
    {
        var currentUserId = context.GetCurrentUserId();
        if (currentUserId is null)
            throw new ForbiddenException("Authentication required.");

        if (context.IsCurrentUserAdmin()) return;

        if (currentUserId.Value != targetUserId)
            throw new ForbiddenException("You are not allowed to access this resource.");
    }
}
