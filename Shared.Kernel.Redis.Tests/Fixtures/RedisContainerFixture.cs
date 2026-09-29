using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Shared.Kernel.Redis.Tests.Fixtures
{
    public class RedisContainerFixture : IAsyncLifetime
    {
        private RedisContainer _container = default!;

        public IConnectionMultiplexer ConnectionMultiplexer { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            _container = new RedisBuilder("redis:7-alpine")
                .Build();

            await _container.StartAsync();

            ConnectionMultiplexer = await global::StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        }

        public async Task DisposeAsync()
        {
            await ConnectionMultiplexer.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    [CollectionDefinition(Name)]
    public class RedisCollection : ICollectionFixture<RedisContainerFixture>
    {
        public const string Name = "Redis";
    }
}
