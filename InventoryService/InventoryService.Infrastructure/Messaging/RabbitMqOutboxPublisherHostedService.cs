using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Contracts.Messaging;
using Shared.Kernel.Database;
using System.Text;
using System.Text.Json;

namespace InventoryService.Infrastructure.Messaging
{
    public class RabbitMqOutboxPublisherHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<RabbitMqSettings> _settings;

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqOutboxPublisherHostedService(IServiceScopeFactory scopeFactory, IOptions<RabbitMqSettings> settings)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settingsValue = _settings.Value;
            var queueName = settingsValue.Queues[RabbitMqQueueNames.InventoryEvents];

            var factory = new ConnectionFactory { HostName = settingsValue.HostName };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false,
                autoDelete: false, cancellationToken: stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settingsValue.PollingIntervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PublishUnprocessedMessagesAsync(queueName, settingsValue.BatchSize, stoppingToken);
            }
        }

        private async Task PublishUnprocessedMessagesAsync(string queueName, int batchSize, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var outboxMessageRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var messages = await outboxMessageRepository.GetUnprocessedAsync(batchSize, cancellationToken);

            foreach (var message in messages)
            {
                var envelope = new BaseEvent(message.EventType, message.Payload);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
                var properties = new BasicProperties { Persistent = true, Type = message.EventType };

                await _channel!.BasicPublishAsync(exchange: string.Empty, routingKey: queueName,
                    mandatory: false, basicProperties: properties, body: body, cancellationToken: cancellationToken);

                await unitOfWork.ExecuteInTransactionAsync(
                    outboxMessageRepository.BuildMarkProcessedOperation(message.Id), cancellationToken);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
