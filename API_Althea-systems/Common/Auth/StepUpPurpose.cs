namespace API_Althea_systems.Common.Auth;

/// <summary>
/// Two flavors of step-up tokens, with very different semantics:
/// <list type="bullet">
///   <item>
///     <see cref="Action"/> — single-use, 60s TTL. Issued for one specific
///     sensitive operation (disable 2FA, change password, etc.). Once
///     presented, the jti is recorded in Redis so the same token can't be
///     replayed even within its 60s validity window.
///   </item>
///   <item>
///     <see cref="Admin"/> — reusable for ~30 min. Granted before entering
///     <c>/api/admin/*</c>; serves every admin call until the user leaves
///     the admin area (front-side React state cleared on layout unmount =
///     prompt again on re-entry, no server-side change required).
///   </item>
/// </list>
/// </summary>
public enum StepUpPurpose
{
    Action,
    Admin,
}

public static class StepUpPurposeExtensions
{
    public static string ToClaim(this StepUpPurpose purpose) => purpose switch
    {
        StepUpPurpose.Action => TokenPurpose.StepUpAction,
        StepUpPurpose.Admin => TokenPurpose.StepUpAdmin,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
    };

    public static TimeSpan GetTtl(this StepUpPurpose purpose) => purpose switch
    {
        StepUpPurpose.Action => TimeSpan.FromSeconds(60),
        StepUpPurpose.Admin => TimeSpan.FromMinutes(30),
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
    };
}
