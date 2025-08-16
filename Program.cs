using DistributedRateLimiter;
using DistributedRateLimiter.Configuration;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("REDIS") ?? "localhost:6379")
);
builder.Services.AddSingleton<RateLimiter>();
builder.Services.AddControllers();
builder.Services
        .Configure<RateLimiterOptions>(builder
                                        .Configuration
                                        .GetSection(nameof(RateLimiterOptions)));

var app = builder.Build();

app.Use(async (context, next) =>
{
    var limiter = context.RequestServices.GetRequiredService<RateLimiter>();
    
    var ip = context.Connection.RemoteIpAddress?.ToString();
    var key = $"rate:{ip}";
    
    const int maxTokens = 5;

    var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync(key);

    context.Response.Headers["X-RateLimit-Limit"] = maxTokens.ToString();
    context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
    context.Response.Headers["X-RateLimit-RetryAfter"] = retryAfterSec.ToString();

    if (!(bool)allowed)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsync($"Rate limit exceeded. Retry after {retryAfterSec} seconds.\n");
        return;
    }

    await next();
});

app.MapGet("/api/data", () => Results.Ok(new { message = "Hello — your request was allowed." }));

app.Run();
