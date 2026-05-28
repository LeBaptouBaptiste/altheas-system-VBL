using StackExchange.Redis;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class RedisStepUpConsumptionStore : IStepUpConsumptionStore
{
    private static string Key(string jti) => $"stepup:consumed:{jti}";

    private readonly IConnectionMultiplexer _redis;

    public RedisStepUpConsumptionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task<bool> TryConsumeAsync(string jti, TimeSpan ttl)
    {
        // SET key 1 NX EX <ttl> -> returns true only if the key did NOT
        // already exist. Atomic on the Redis side, no race window.
        return _redis.GetDatabase().StringSetAsync(
            Key(jti),
            "1",
            ttl,
            when: When.NotExists);
    }
}
