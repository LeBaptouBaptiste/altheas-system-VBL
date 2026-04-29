using StackExchange.Redis;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class RedisTwoFactorStateStore : ITwoFactorStateStore
{
    private static string SetupKey(Guid userId) => $"2fa:setup:{userId}";
    private static string ReplayKey(Guid userId) => $"2fa:replay:{userId}";
    private static string FailureKey(Guid userId) => $"2fa:failed:{userId}";
    private static string LockKey(Guid userId) => $"2fa:lock:{userId}";

    private readonly IConnectionMultiplexer _redis;

    public RedisTwoFactorStateStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.GetDatabase();

    public Task SetSetupSecretAsync(Guid userId, string base32Secret, TimeSpan ttl)
        => Db.StringSetAsync(SetupKey(userId), base32Secret, ttl);

    public async Task<string?> GetSetupSecretAsync(Guid userId)
    {
        var v = await Db.StringGetAsync(SetupKey(userId));
        return v.IsNullOrEmpty ? null : v.ToString();
    }

    public Task DeleteSetupSecretAsync(Guid userId)
        => Db.KeyDeleteAsync(SetupKey(userId));

    public async Task<long?> GetLastReplayStepAsync(Guid userId)
    {
        var v = await Db.StringGetAsync(ReplayKey(userId));
        if (v.IsNullOrEmpty) return null;
        return long.TryParse((string?)v, out var step) ? step : null;
    }

    public Task SetLastReplayStepAsync(Guid userId, long step, TimeSpan ttl)
        => Db.StringSetAsync(ReplayKey(userId), step.ToString(), ttl);

    public Task DeleteReplayStepAsync(Guid userId)
        => Db.KeyDeleteAsync(ReplayKey(userId));

    // ─────────────────────────────────────────────────────────
    //  Failure counter + lock
    // ─────────────────────────────────────────────────────────

    public async Task<int> IncrementFailureAsync(Guid userId, TimeSpan ttl)
    {
        var key = FailureKey(userId);
        var newCount = await Db.StringIncrementAsync(key);
        // INCR creates the key with no expiry on first write — set the TTL
        // explicitly the first time so the counter doesn't live forever.
        if (newCount == 1)
        {
            await Db.KeyExpireAsync(key, ttl);
        }
        return (int)newCount;
    }

    public Task ResetFailuresAsync(Guid userId)
        => Db.KeyDeleteAsync(FailureKey(userId));

    public Task LockAsync(Guid userId, TimeSpan ttl)
        => Db.StringSetAsync(LockKey(userId), "1", ttl);

    public async Task<TimeSpan?> GetLockRemainingAsync(Guid userId)
    {
        var key = LockKey(userId);
        // KEY existence + TTL in a single trip would require Lua; two
        // round-trips here are fine — auth flows aren't latency-sensitive
        // and the same operation already issues several Redis calls.
        var ttl = await Db.KeyTimeToLiveAsync(key);
        return ttl;
    }
}
