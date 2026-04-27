namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Tracks which step-up tokens (by jti) have been consumed, to enforce
/// single-use semantics on <c>StepUpPurpose.Action</c> tokens.
/// Backed by Redis in production via <see cref="Services.RedisStepUpConsumptionStore"/>.
/// </summary>
public interface IStepUpConsumptionStore
{
    /// <summary>
    /// Atomically marks the jti as consumed. Returns <c>true</c> if this
    /// is the first time we see it (caller may proceed), <c>false</c> if
    /// it was already consumed (caller must reject as replay).
    /// </summary>
    /// <param name="jti">The token identifier (must be unique per token).</param>
    /// <param name="ttl">How long to remember the jti — typically the token's TTL.</param>
    Task<bool> TryConsumeAsync(string jti, TimeSpan ttl);
}
