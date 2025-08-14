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
        var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync("user:1", 5, 1);

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
        var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync("user:1", 5, 1);

        // Assert
        Assert.True(allowed);
        Assert.Equal(2, remaining);
        Assert.Equal(0, retryAfterSec);
    }
    
    private static RateLimiter CreateLimiter(IDatabase db)
    {
        var muxerMock = new Mock<IConnectionMultiplexer>();
        muxerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db);

        return new RateLimiter(muxerMock.Object);
    }
}