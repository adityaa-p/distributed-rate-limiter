using DistributedRateLimiter.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DistributedRateLimiter;

public class RateLimiter(IConnectionMultiplexer redis, IOptions<RateLimiterOptions> rateLimiterOptions)
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<(bool allowed, double remaining, int retryAfterSec)> AllowRequestAsync(string key)
    {
        var bucket = await _database.HashGetAsync(key, ["tokens", "lastRefill"]);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var tokens = rateLimiterOptions.Value.BurstCapacity;
        var lastRefill = now;

        if (!bucket[0].IsNull && !bucket[1].IsNull)
        {
            tokens = (double)bucket[0];
            lastRefill = (long)bucket[1];
        }
        
        var delta = Math.Max(0, now - lastRefill);
        tokens = Math.Min(rateLimiterOptions.Value.BurstCapacity, tokens + delta * rateLimiterOptions.Value.RefillPerSecond);

        if (tokens >= 1)
        {
            tokens -= 1;
            await _database.HashSetAsync(key, [
                new HashEntry("tokens", tokens),
                new HashEntry("lastRefill", now)
            ]);
            return (true, tokens, 0);
        }
        else
        {
            var retryAfter = (int)Math.Ceiling((1 - tokens) / rateLimiterOptions.Value.RefillPerSecond);
            return (false, tokens, retryAfter);
        }
    }
}
