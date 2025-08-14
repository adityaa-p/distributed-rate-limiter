using StackExchange.Redis;

namespace DistributedRateLimiter;

public class RateLimiter(IConnectionMultiplexer redis)
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<(bool allowed, double remaining, int retryAfterSec)> AllowRequestAsync(string key, double burstCapacity, double refillPerSecond, int tokenRequested = 1)
    {
        var bucket = await _database.HashGetAsync(key, ["token", "lastRefill"]);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var tokens = burstCapacity;
        var lastRefill = now;

        if (!bucket[0].IsNull && !bucket[1].IsNull)
        {
            tokens = (double)bucket[0];
            lastRefill = (long)bucket[1];
        }
        
        var delta = Math.Max(0, now - lastRefill);
        tokens = Math.Min(burstCapacity, tokens + delta * refillPerSecond);

        if (tokens >= 1)
        {
            tokens -= 1;
            await _database.HashSetAsync(key, [
                new HashEntry("tokens", tokens),
                new HashEntry("lastRefill", now)
            ]);
            await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(burstCapacity / refillPerSecond));
            return (true, tokens, 0);
        }
        else
        {
            var retryAfter = (int)Math.Ceiling((1 - tokens) / refillPerSecond);
            return (false, tokens, retryAfter);
        }
    }
}
