using InventoryService.Application.Services.EventHandling;
using InventoryService.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messaging;
using System.Text.Json;

namespace InventoryService.Infrastructure.Messaging
{
    public class RabbitMqConsumerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<RabbitMqSettings> _settings;

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqConsumerHostedService(IServiceScopeFactory scopeFactory, IOptions<RabbitMqSettings> settings)
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

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<BaseEvent>(eventArgs.Body.Span)
                    ?? throw new InvalidOperationException("Empty message envelope.");

                using var scope = _scopeFactory.CreateScope();
                var handlerFactory = scope.ServiceProvider.GetRequiredService<IInventoryEventHandlerFactory>();
                var handler = handlerFactory.GetHandler(envelope.NameKey);

                if (handler is null)
                {
                    // Unknown NameKey is a permanent failure (no future retry will make a handler appear) — dead-letter, not requeue.
                    await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await handler.HandleAsync(envelope.Payload, CancellationToken.None);

                await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch
            {
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
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
