namespace DistributedRateLimiter.Configuration;

public class RateLimiterOptions
{
    public string Name { get; set; } = nameof(RateLimiter);

    public double BurstCapacity { get; set; }

    public int RefillPerSecond { get; set; }
}