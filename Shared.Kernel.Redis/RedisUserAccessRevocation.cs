using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public class RedisUserAccessRevocation : IUserAccessRevocation
    {
        private const string KeyPrefix = "revoked-access:";

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisUserAccessRevocation(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public Task RevokeAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();

            return database.StringSetAsync(KeyPrefix + userId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ttl);
        }

        public async Task<DateTimeOffset?> GetRevokedAtAsync(Guid userId, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var value = await database.StringGetAsync(KeyPrefix + userId);

            if (!value.HasValue)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds((long)value);
        }
    }
}
