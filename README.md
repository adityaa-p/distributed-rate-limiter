# Distributed Rate Limiter

This project implements a distributed token bucket rate limiter in C# with Redis as the backend store.
It supports two interchangeable implementations:

1. C# Logic – rate-limiting math is handled in application code, using multiple Redis calls per request (HashGet, HashSet, KeyExpire).

2. Lua Script – token bucket logic is executed atomically inside Redis with a single EVAL call, reducing round trips and ensuring stronger consistency under high concurrency.

Both implementations expose the same method:

`var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync(key, maxTokens, refillPerSecond);`

Middleware integrates this into the ASP.NET Core pipeline, applying per-user limits and returning standard rate-limit headers (X-RateLimit-*).
