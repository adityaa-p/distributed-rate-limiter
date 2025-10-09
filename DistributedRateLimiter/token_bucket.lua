-- KEYS[1]   = bucket key
-- ARGV[1]   = now (ms)
-- ARGV[2]   = refill rate (tokens/sec)
-- ARGV[3]   = burst capacity (max tokens)
-- ARGV[4]   = tokens requested (usually 1)

local key = KEYS[1]
local now = tonumber(ARGV[1])
local rate = tonumber(ARGV[2])
local burst = tonumber(ARGV[3])
local requested = tonumber(ARGV[4])

-- Read existing state
local data = redis.call("HMGET", key, "tokens", "last_ts")
local tokens = tonumber(data[1])
local last_ts = tonumber(data[2])

if tokens == nil then
    tokens = burst -- start full bucket
    last_ts = now
end

-- Refill tokens based on elapsed time
local delta = math.max(0, now - last_ts) / 1000.0
local refill = delta * rate
tokens = math.min(burst, tokens + refill)
tokens = math.floor(tokens)

-- Check if enough tokens
if tokens >= requested then
    tokens = tokens - requested
    last_ts = now
    redis.call("HMSET", key, "tokens", tokens, "last_ts", last_ts)
--     redis.call("PEXPIRE", key, math.ceil((burst / rate) * 1000))
    return {1, tokens, 0}  -- allowed=1, tokens remaining, retry_after=0
else
    local retry_after = math.ceil((requested - tokens) / rate)
    redis.call("HMSET", key, "tokens", tokens, "last_ts", now)
--     redis.call("PEXPIRE", key, math.ceil((burst / rate) * 1000))
    return {0, tokens, retry_after}  -- allowed=0, tokens remaining, retry_after seconds
end
