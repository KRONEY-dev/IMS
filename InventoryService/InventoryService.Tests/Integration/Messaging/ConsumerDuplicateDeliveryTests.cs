using InventoryService.Application.Services.EventHandling;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Options;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Contracts.Events;
using Shared.Contracts.Messaging;
using System.Text;
using System.Text.Json;
using Xunit;

namespace InventoryService.Tests.Integration.Messaging
{
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class ConsumerDuplicateDeliveryTests
    {
        private readonly PostgresContainerFixture _postgres;
        private readonly RabbitMqContainerFixture _rabbitMq;

        public ConsumerDuplicateDeliveryTests(PostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq)
        {
            _postgres = postgres;
            _rabbitMq = rabbitMq;
        }

        [Fact]
        public async Task Handler_InvokedTwiceForSameEvent_CreatesOnlyOneActiveAlert()
        {
            var (productId, warehouseId) = await SeedBelowThresholdAsync();
            var payload = JsonSerializer.Serialize(new StockQuantityChangedEvent(productId, warehouseId, Increased: false));

            await InvokeHandlerAsync(payload);
            await InvokeHandlerAsync(payload);

            var activeAlertCount = await CountActiveAlertsAsync(productId, warehouseId);
            Assert.Equal(1, activeAlertCount);
        }

        [Fact]
        public async Task ConsumerHostedService_DuplicateMessage_StillResultsInOneActiveAlert()
        {
            var (productId, warehouseId) = await SeedBelowThresholdAsync();
            var queueName = $"test-consumer-{Guid.NewGuid()}";
            var body = BuildMessageBody(productId, warehouseId);

            using (var connection = await _rabbitMq.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

                var properties = new BasicProperties { Persistent = true };

                // Publishing the same event body twice simulates exactly what at-least-once
                // redelivery produces from the consumer's point of view: the identical message
                // handled more than once.
                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName,
                    mandatory: false, basicProperties: properties, body: body);
                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName,
                    mandatory: false, basicProperties: properties, body: body);
            }

            var settings = Options.Create(new RabbitMqSettings
            {
                HostName = _rabbitMq.Hostname,
                Port = _rabbitMq.Port,
                Queues = { [RabbitMqQueueNames.InventoryEvents] = queueName }
            });

            var consumer = new RabbitMqConsumerHostedService(_postgres.ScopeFactory, settings);

            await consumer.StartAsync(CancellationToken.None);

            try
            {
                await WaitUntilAsync(async () => await GetQueueMessageCountAsync(queueName) == 0, TimeSpan.FromSeconds(15));
            }
            finally
            {
                await consumer.StopAsync(CancellationToken.None);
                consumer.Dispose();
            }

            var activeAlertCount = await CountActiveAlertsAsync(productId, warehouseId);
            Assert.Equal(1, activeAlertCount);
        }

        private async Task<(Guid ProductId, Guid WarehouseId)> SeedBelowThresholdAsync()
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            return await InventorySeeding.SeedBelowThresholdAsync(dbContext);
        }

        private async Task InvokeHandlerAsync(string payload)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var handlerFactory = scope.ServiceProvider.GetRequiredService<IInventoryEventHandlerFactory>();
            var handler = handlerFactory.GetHandler(nameof(StockQuantityChangedEvent))
                ?? throw new InvalidOperationException("No handler registered for StockQuantityChangedEvent.");

            await handler.HandleAsync(payload, CancellationToken.None);
        }

        private async Task<int> CountActiveAlertsAsync(Guid productId, Guid warehouseId)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            return await dbContext.LowStockAlerts.CountAsync(alert =>
                alert.ProductId == productId && alert.WarehouseId == warehouseId
                && alert.Status == LowStockAlertStatus.Active);
        }

        private static byte[] BuildMessageBody(Guid productId, Guid warehouseId)
        {
            var eventPayload = JsonSerializer.Serialize(new StockQuantityChangedEvent(productId, warehouseId, Increased: false));
            var envelope = new BaseEvent(nameof(StockQuantityChangedEvent), eventPayload);

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        }

        private async Task<uint> GetQueueMessageCountAsync(string queueName)
        {
            using var connection = await _rabbitMq.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var declareOk = await channel.QueueDeclarePassiveAsync(queueName);

            return declareOk.MessageCount;
        }

        private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline)
            {
                if (await condition())
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }
    }
}
