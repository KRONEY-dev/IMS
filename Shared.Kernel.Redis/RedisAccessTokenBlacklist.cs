using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public class RedisAccessTokenBlacklist(IConnectionMultiplexer connectionMultiplexer) : IAccessTokenBlacklist
    {
        private const string KeyPrefix = "blacklist:jti:";

        public Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken)
        {
            var database = connectionMultiplexer.GetDatabase();

            return database.StringSetAsync(KeyPrefix + jti, true, ttl);
        }

        public Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken)
        {
            var database = connectionMultiplexer.GetDatabase();

            return database.KeyExistsAsync(KeyPrefix + jti);
        }
    }
}