using StackExchange.Redis;

namespace InventoryService.Infrastructure.RealTime
{
    public class RedisUserConnectionRegistry : IUserConnectionRegistry
    {
        private const string KeyPrefix = "inventory-hub-connections:";

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisUserConnectionRegistry(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public Task AddConnectionAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();

            return database.SetAddAsync(KeyPrefix + userId, connectionId);
        }

        public Task RemoveConnectionAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();

            return database.SetRemoveAsync(KeyPrefix + userId, connectionId);
        }

        public async Task<IReadOnlyList<string>> GetConnectionIdsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var members = await database.SetMembersAsync(KeyPrefix + userId);

            return members.Select(member => member.ToString()).ToList();
        }
    }
}
