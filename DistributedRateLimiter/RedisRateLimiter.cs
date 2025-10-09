using DistributedRateLimiter.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DistributedRateLimiter;

public class RedisRateLimiter
{
    private readonly IOptions<RateLimiterOptions> _rateLimiterOptions;
    private readonly IDatabase _db;
    private readonly LoadedLuaScript _script;
    private readonly string _scriptContent; // Cache the script content

    public RedisRateLimiter(IConnectionMultiplexer redis, IOptions<RateLimiterOptions> rateLimiterOptions)
    {
        _rateLimiterOptions = rateLimiterOptions;
        _db = redis.GetDatabase();

        _scriptContent = File.ReadAllText("token_bucket.lua");
        var prepared = LuaScript.Prepare(_scriptContent);
        
        // Ensure script is loaded
        var server = redis.GetServer(redis.GetEndPoints()[0]);
        _script = prepared.Load(server);
    }

    public async Task<(bool Allowed, int RemainingTokens, int RetryAfter)> AllowRequestAsync(string key)
    {
        var tokensToConsume = 1;
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        RedisResult[] result;
    
        try
        {
            result = (RedisResult[])(await _db.ScriptEvaluateAsync(
                _script.Hash,
                new RedisKey[] { key },
                new RedisValue[] { 
                    nowMs, 
                    _rateLimiterOptions.Value.RefillPerSecond, 
                    _rateLimiterOptions.Value.BurstCapacity, 
                    tokensToConsume 
                }
            ))!;
        }
        catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT"))
        {
            result = (RedisResult[])(await _db.ScriptEvaluateAsync(
                _scriptContent,
                new RedisKey[] { key },
                new RedisValue[] { 
                    nowMs, 
                    _rateLimiterOptions.Value.RefillPerSecond, 
                    _rateLimiterOptions.Value.BurstCapacity, 
                    tokensToConsume 
                }
            ))!;
        }

        var allowed = (long)result[0] == 1;
        var remaining = (int)Math.Floor((double)(long)result[1]);
        var retryAfter = (int)(long)result[2];

        return (allowed, remaining, retryAfter);
    }
}