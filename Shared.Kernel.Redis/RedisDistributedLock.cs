using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public class RedisDistributedLock : IDistributedLock
    {
        private const string KeyPrefix = "lock:job:";

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisDistributedLock(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public Task<bool> TryAcquireAsync(string key, string instanceId, TimeSpan ttl, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();

            return database.StringSetAsync(KeyPrefix + key, instanceId, ttl, When.NotExists);
        }
    }
}
