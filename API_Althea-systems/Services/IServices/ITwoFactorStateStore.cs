namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Volatile per-user state used by <see cref="ITwoFactorService"/>:
///   - the pending TOTP secret during setup (cleared on enable / TTL)
///   - the last successful TOTP step for replay protection
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
}
