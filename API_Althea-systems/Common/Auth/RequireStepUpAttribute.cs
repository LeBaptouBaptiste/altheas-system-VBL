using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Common.Auth;

/// <summary>
/// Authorization filter that gates an endpoint behind a fresh step-up token.
/// The caller MUST be authenticated (regular access token, populated by
/// JwtMiddleware into <c>HttpContext.Items["UserId"]</c>) AND must include
/// a valid step-up token in the <c>X-Step-Up-Token</c> header.
///
/// Validation rules:
///   1. Header present and parses as a JWT with the expected purpose.
///   2. The token's <c>sub</c> matches the authenticated user (no cross-user replay).
///   3. For <see cref="StepUpPurpose.Action"/>: the jti must not have been
///      consumed yet (atomic check via <see cref="IStepUpConsumptionStore"/>).
///      Admin step-up tokens are reusable and skip this check.
///
/// On any failure, returns 403 with <c>{ reason: "step_up_required" }</c>
/// so the front-end can pop a step-up modal and retry.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireStepUpAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string HeaderName = "X-Step-Up-Token";

    public StepUpPurpose Purpose { get; }

    public RequireStepUpAttribute(StepUpPurpose purpose)
    {
        Purpose = purpose;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;

        // Caller must already be authenticated with a normal access token.
        var userIdStr = http.Items["UserId"]?.ToString();
        if (!Guid.TryParse(userIdStr, out var authedUserId))
        {
            context.Result = StepUpRequiredResult();
            return;
        }

        if (!http.Request.Headers.TryGetValue(HeaderName, out var headerValues)
            || string.IsNullOrWhiteSpace(headerValues.ToString()))
        {
            context.Result = StepUpRequiredResult();
            return;
        }
        var token = headerValues.ToString().Trim();

        var tokenService = http.RequestServices.GetRequiredService<ITokenService>();
        var validation = tokenService.ValidateStepUpToken(token, Purpose);
        if (validation is null)
        {
            context.Result = StepUpRequiredResult();
            return;
        }

        // Cross-user replay protection.
        if (validation.UserId != authedUserId)
        {
            context.Result = StepUpRequiredResult();
            return;
        }

        // Single-use enforcement for Action tokens; Admin tokens are reusable.
        if (Purpose == StepUpPurpose.Action)
        {
            var store = http.RequestServices.GetRequiredService<IStepUpConsumptionStore>();
            var firstUse = await store.TryConsumeAsync(validation.Jti, Purpose.GetTtl());
            if (!firstUse)
            {
                context.Result = StepUpRequiredResult();
                return;
            }
        }

        // All good — let the action proceed.
    }

    private static ObjectResult StepUpRequiredResult() =>
        new(new { reason = "step_up_required" }) { StatusCode = StatusCodes.Status403Forbidden };
}
