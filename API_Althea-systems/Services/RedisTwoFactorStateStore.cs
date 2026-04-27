using StackExchange.Redis;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class RedisTwoFactorStateStore : ITwoFactorStateStore
{
    private static string SetupKey(Guid userId) => $"2fa:setup:{userId}";
    private static string ReplayKey(Guid userId) => $"2fa:replay:{userId}";

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
}
