using InventoryService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InventoryService.Infrastructure.Messaging
{
    public sealed class RabbitMqHealthCheckConnection
    {
        private readonly ConnectionFactory _factory;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private IConnection? _connection;

        public RabbitMqHealthCheckConnection(IOptions<RabbitMqSettings> settings)
        {
            _factory = RabbitMqConnectionFactory.Build(settings.Value);
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            await _gate.WaitAsync();

            try
            {
                if (_connection is not { IsOpen: true })
                {
                    _connection?.Dispose();
                    _connection = await _factory.CreateConnectionAsync();
                }

                return _connection;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
