namespace DistributedRateLimiter;

public class RateLimiter
{
    public async Task<(object allowed, object remaining, object retryAfterSec)> AllowRequestAsync(string key, int maxTokens, double refillPerSecond)
    {
        throw new NotImplementedException();
    }
}