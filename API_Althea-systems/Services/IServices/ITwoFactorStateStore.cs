namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Volatile per-user state used by <see cref="ITwoFactorService"/>:
///   - the pending TOTP secret during setup (cleared on enable / TTL)
///   - the last successful TOTP step for replay protection
///   - the failed-attempt counter and lock for brute-force protection
///
/// Backed by Redis in production (<see cref="Services.RedisTwoFactorStateStore"/>),
/// trivially fakeable in unit tests.
/// </summary>
public interface ITwoFactorStateStore
{
    Task SetSetupSecretAsync(Guid userId, string base32Secret, TimeSpan ttl);
    Task<string?> GetSetupSecretAsync(Guid userId);
    Task DeleteSetupSecretAsync(Guid userId);

    Task<long?> GetLastReplayStepAsync(Guid userId);
    Task SetLastReplayStepAsync(Guid userId, long step, TimeSpan ttl);
    Task DeleteReplayStepAsync(Guid userId);

    /// <summary>
    /// Atomically increments the failure counter for the user. The first
    /// failure (returned value == 1) sets the TTL so the counter
    /// auto-resets if no further failures arrive within the window.
    /// </summary>
    Task<int> IncrementFailureAsync(Guid userId, TimeSpan ttl);

    /// <summary>Resets the counter — call after a successful verification.</summary>
    Task ResetFailuresAsync(Guid userId);

    /// <summary>Locks the account for <paramref name="ttl"/>.</summary>
    Task LockAsync(Guid userId, TimeSpan ttl);

    /// <summary>
    /// Returns the remaining lock duration if the account is currently
    /// locked, or null if it isn't.
    /// </summary>
    Task<TimeSpan?> GetLockRemainingAsync(Guid userId);
}
