using StackExchange.Redis;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

/// <summary>
/// Redis-backed implementation of <see cref="ILoginAttemptStore"/>.
/// Keys are namespaced with the lower-cased email so the same address
/// always maps to the same counter regardless of casing. Patterned on
/// <see cref="RedisTwoFactorStateStore"/>.
/// </summary>
public class RedisLoginAttemptStore : ILoginAttemptStore
{
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
    private static string FailureKey(string email) => $"login:failed:{Normalize(email)}";
    private static string LockKey(string email) => $"login:lock:{Normalize(email)}";

    private readonly IConnectionMultiplexer _redis;

    public RedisLoginAttemptStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.GetDatabase();

    public async Task<TimeSpan?> GetLockRemainingAsync(string email)
        => await Db.KeyTimeToLiveAsync(LockKey(email));

    public async Task<int> IncrementFailureAsync(string email, TimeSpan ttl)
    {
        var key = FailureKey(email);
        var newCount = await Db.StringIncrementAsync(key);
        // INCR creates the key with no TTL on first hit; set it explicitly
        // so the counter eventually evicts.
        if (newCount == 1)
        {
            await Db.KeyExpireAsync(key, ttl);
        }
        return (int)newCount;
    }

    public Task LockAsync(string email, TimeSpan ttl)
        => Db.StringSetAsync(LockKey(email), "1", ttl);

    public async Task ResetAsync(string email)
    {
        await Db.KeyDeleteAsync(FailureKey(email));
        await Db.KeyDeleteAsync(LockKey(email));
    }
}

/// <summary>
/// No-op fallback used when Redis is not configured (typically dev/test).
/// All operations succeed silently so authentication still works; rate
/// limiting simply degrades to the IP-based <c>"auth"</c> policy.
/// </summary>
public class NullLoginAttemptStore : ILoginAttemptStore
{
    public Task<TimeSpan?> GetLockRemainingAsync(string email)
        => Task.FromResult<TimeSpan?>(null);

    public Task<int> IncrementFailureAsync(string email, TimeSpan ttl)
        => Task.FromResult(0);

    public Task LockAsync(string email, TimeSpan ttl) => Task.CompletedTask;

    public Task ResetAsync(string email) => Task.CompletedTask;
}
