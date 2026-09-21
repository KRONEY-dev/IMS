using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public class RedisRateLimiter(IConnectionMultiplexer connectionMultiplexer) : IRateLimiter
    {
        private const string KeyPrefix = "rate-limit:";

        public async Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken)
        {
            var database = connectionMultiplexer.GetDatabase();
            var redisKey = KeyPrefix + key;

            var count = await database.StringIncrementAsync(redisKey);

            if (count == 1)
            {
                await database.KeyExpireAsync(redisKey, window);
            }

            return count <= limit;
        }
    }
}
