using DistributedRateLimiter.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace DistributedRateLimiter.Tests;

public class RateLimiterTests
{
    [Fact]
    public async Task FirstRequest_ShouldInitializeBucket_AndAllow()
    {
        // Arrange
        var databaseMock = new Mock<IDatabase>();
        databaseMock.Setup(db => db.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), CommandFlags.None))
            .ReturnsAsync([RedisValue.Null, RedisValue.Null]);

        var limiter = CreateLimiter(databaseMock.Object);

        // Act
        var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync("user:1");

        // Assert
        Assert.True(allowed);
        Assert.Equal(4, remaining);
        Assert.Equal(0, retryAfterSec);
        databaseMock.Verify(db => db.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None), Times.Once);
    }
    
    [Fact]
    public async Task RequestWithTokensRemaining_ShouldAllow()
    {
        // Arrange
        var dbMock = new Mock<IDatabase>();
        var now = DateTimeOffset
                        .UtcNow
                        .ToUnixTimeSeconds();
        dbMock
            .Setup(db => db.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), CommandFlags.None))
            .ReturnsAsync([3, now]);

        var limiter = CreateLimiter(dbMock.Object);

        // Act
        var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync("user:1");

        // Assert
        Assert.True(allowed);
        Assert.Equal(2, remaining);
        Assert.Equal(0, retryAfterSec);
    }
    
    [Fact]
    public async Task RequestWhenEmpty_ShouldDeny()
    {
        // Arrange
        var dbMock = new Mock<IDatabase>();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        dbMock.Setup(db => db.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), CommandFlags.None))
            .ReturnsAsync([0, now]);

        var limiter = CreateLimiter(dbMock.Object);

        // Act
        var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync("user:1");

        // Assert
        Assert.False(allowed);
        Assert.Equal(0, remaining);
        Assert.True(retryAfterSec >= 1);
    }
    
    private static RateLimiter CreateLimiter(IDatabase db)
    {
        var muxerMock = new Mock<IConnectionMultiplexer>();
        
        var rateLimiterOptions = new RateLimiterOptions { BurstCapacity = 5, RefillPerSecond = 1};
        var options = Options.Create(rateLimiterOptions);

        muxerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db);

        return new RateLimiter(muxerMock.Object, options);
    }
}