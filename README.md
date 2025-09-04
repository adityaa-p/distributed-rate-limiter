# Distributed Rate Limiter

This project implements a distributed token bucket rate limiter in C# with Redis as the backend store.
It supports two interchangeable implementations:

1. C# Logic – rate-limiting math is handled in application code, using multiple Redis calls per request (HashGet, HashSet, KeyExpire).

2. Lua Script – token bucket logic is executed atomically inside Redis with a single EVAL call, reducing round trips and ensuring stronger consistency under high concurrency.

Both implementations expose the same method:

`var (allowed, remaining, retryAfterSec) = await limiter.AllowRequestAsync(key, maxTokens, refillPerSecond);`

Middleware integrates this into the ASP.NET Core pipeline, applying per-user limits and returning standard rate-limit headers (X-RateLimit-*).

# Benchmark Example

I benchmarked the C# logic implementation with hey on an Apple M2 (8 cores, 16 GB RAM).
The API and Redis were running inside Podman containers.

Test parameters:

* Total requests: 50,000
* Concurrency: 100
* Endpoint: /api/data (single key)

Results:

* Throughput: ~4,000 requests/sec
* Average latency: 25 ms per request
* p99 latency: ~17 ms
* Total time: ~12.5 seconds

Observation: 99.8% of requests completed in <20 ms. A few outliers (~100 requests) took up to 9s, likely due to Redis queuing and container networking overhead.
