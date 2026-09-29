using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
    public class RabbitMqContainerFixture : IAsyncLifetime
    {
        private RabbitMqContainer _container = default!;

        public string Hostname { get; private set; } = default!;
        public int Port { get; private set; }

        public async Task InitializeAsync()
        {
            _container = new RabbitMqBuilder("rabbitmq:4-management-alpine")
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();

            await _container.StartAsync();

            Hostname = _container.Hostname;
            Port = _container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort);
        }

        public async Task<IConnection> CreateConnectionAsync()
        {
            var factory = new ConnectionFactory { HostName = Hostname, Port = Port };

            return await factory.CreateConnectionAsync();
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }
    }
}