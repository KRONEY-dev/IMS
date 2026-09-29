using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Enums;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Events;
using Shared.Contracts.Messaging;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace InventoryService.Tests.Integration.RealTime
{
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class UserAccessChangeSubscriberTests
    {
        private readonly InventoryApiFactory _factory;

        public UserAccessChangeSubscriberTests(
            ApiHostPostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq, RedisContainerFixture redis, InventoryApiFactory factory)
        {
            factory.PostgresConnectionString = postgres.ConnectionString;
            factory.RedisConnectionString = redis.ConnectionString;
            factory.RabbitMqHostName = rabbitMq.Hostname;
            factory.RabbitMqPort = rabbitMq.Port;

            _factory = factory;
        }

        [Fact]
        public async Task WarehouseAccessGranted_ViaRedisEvent_StartsReceivingThatWarehousesNotifications()
        {
            var userId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            await using var connection = BuildConnection(TestJwtFactory.CreateToken(userId, UserRole.Admin));
            var received = NextNotification(connection);

            await connection.StartAsync();
            await PublishAccessChangedAsync(userId, warehouseId, added: true);
            await WaitForSubscriberToProcessAsync();

            await PublishStockLevelChangedAsync(warehouseId);

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(received.Task, completed);
            Assert.Equal(warehouseId, (await received.Task).WarehouseId);
        }

        [Fact]
        public async Task WarehouseAccessRevoked_ViaRedisEvent_StopsReceivingThatWarehousesNotifications()
        {
            var userId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            await using var connection = BuildConnection(TestJwtFactory.CreateToken(userId, UserRole.Admin));
            await connection.StartAsync();
            await connection.InvokeAsync("JoinWarehouse", warehouseId);

            await PublishAccessChangedAsync(userId, warehouseId, added: false);
            await WaitForSubscriberToProcessAsync();

            var received = NextNotification(connection);
            await PublishStockLevelChangedAsync(warehouseId);

            var completedTooSoon = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.NotSame(received.Task, completedTooSoon);
        }

        [Fact]
        public async Task AccessRevoked_ViaRedisEvent_ForcesTheConnectionToDisconnect()
        {
            var userId = Guid.NewGuid();

            await using var connection = BuildConnection(TestJwtFactory.CreateToken(userId, UserRole.Admin));

            var forceDisconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.On("ForceDisconnect", () => forceDisconnected.TrySetResult());

            await connection.StartAsync();
            await PublishAccessRevokedAsync(userId);

            var completed = await Task.WhenAny(forceDisconnected.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(forceDisconnected.Task, completed);
        }

        private static TaskCompletionSource<NotificationServiceDTOs.StockLevelChangedNotification> NextNotification(HubConnection connection)
        {
            var received = new TaskCompletionSource<NotificationServiceDTOs.StockLevelChangedNotification>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            connection.On<NotificationServiceDTOs.StockLevelChangedNotification>("StockLevelChanged", notification =>
            {
                received.TrySetResult(notification);
            });

            return received;
        }

        private static Task WaitForSubscriberToProcessAsync()
        {
            return Task.Delay(TimeSpan.FromSeconds(1));
        }

        private async Task PublishAccessChangedAsync(Guid userId, Guid warehouseId, bool added)
        {
            var payload = JsonSerializer.Serialize(new UserWarehouseAccessChangedEvent(userId, warehouseId, added));
            await GetRedisSubscriber().PublishAsync(RedisChannel.Literal(RedisChannels.UserWarehouseChanged), payload);
        }

        private async Task PublishAccessRevokedAsync(Guid userId)
        {
            var payload = JsonSerializer.Serialize(new UserAccessRevokedEvent(userId));
            await GetRedisSubscriber().PublishAsync(RedisChannel.Literal(RedisChannels.UserAccessRevoked), payload);
        }

        private ISubscriber GetRedisSubscriber()
        {
            return _factory.Services.GetRequiredService<IConnectionMultiplexer>().GetSubscriber();
        }

        private async Task PublishStockLevelChangedAsync(Guid warehouseId)
        {
            using var scope = _factory.Services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

            await publisher.NotifyStockLevelChangedAsync(
                new NotificationServiceDTOs.StockLevelChangedNotification(Guid.NewGuid(), warehouseId), CancellationToken.None);
        }

        private HubConnection BuildConnection(string token)
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/inventory"), options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();
        }
    }
}