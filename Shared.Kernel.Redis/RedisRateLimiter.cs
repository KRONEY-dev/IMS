using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public class RedisRateLimiter(IConnectionMultiplexer connectionMultiplexer) : IRateLimiter
    {
        private const string KeyPrefix = "rate-limit:";

        private static readonly LuaScript IncrementAndExpireScript = LuaScript.Prepare(
            "local count = redis.call('INCR', @key) " +
            "if count == 1 then redis.call('PEXPIRE', @key, @windowMs) end " +
            "return count");

        public async Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken)
        {
            var database = connectionMultiplexer.GetDatabase();
            var redisKey = KeyPrefix + key;

            var count = (int)await database.ScriptEvaluateAsync(
                IncrementAndExpireScript, new { key = (RedisKey)redisKey, windowMs = (long)window.TotalMilliseconds });

            return count <= limit;
        }
    }
}
