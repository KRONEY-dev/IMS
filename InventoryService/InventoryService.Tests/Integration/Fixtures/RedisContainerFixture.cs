using Testcontainers.Redis;
using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
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