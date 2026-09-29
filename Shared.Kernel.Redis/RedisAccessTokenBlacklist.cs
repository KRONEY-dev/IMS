using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public class RedisAccessTokenBlacklist : IAccessTokenBlacklist
    {
        private const string KeyPrefix = "blacklist:jti:";

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisAccessTokenBlacklist(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();

            return database.StringSetAsync(KeyPrefix + jti, true, ttl);
        }

        public Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();

            return database.KeyExistsAsync(KeyPrefix + jti);
        }
    }
}