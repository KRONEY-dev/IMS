using Testcontainers.Redis;
using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
    // Real Redis (matches docker-compose.yml's redis image), one container shared across the
    // "Integration" collection. Needed for the SignalR Redis backplane and IDistributedLock
    // that InventoryApiFactory's full app host wires up exactly as production does.
    public class RedisContainerFixture : IAsyncLifetime
    {
        private RedisContainer _container = default!;

        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            _container = new RedisBuilder("redis:7-alpine")
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }
    }
}
