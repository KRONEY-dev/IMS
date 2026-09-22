using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Enums;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryService.Tests.Integration.RealTime
{
    // Boots the real InventoryService.API host and connects a real SignalR client to it, proving
    // that InventoryHub.JoinWarehouse actually scopes broadcasts to the joined warehouse's group -
    // not just that the group-name string is computed consistently on both ends.
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class SignalRGroupMembershipTests
    {
        private readonly InventoryApiFactory _factory;

        public SignalRGroupMembershipTests(
            ApiHostPostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq, RedisContainerFixture redis, InventoryApiFactory factory)
        {
            factory.PostgresConnectionString = postgres.ConnectionString;
            factory.RedisConnectionString = redis.ConnectionString;
            factory.RabbitMqHostName = rabbitMq.Hostname;
            factory.RabbitMqPort = rabbitMq.Port;

            _factory = factory;
        }

        [Fact]
        public async Task JoinWarehouse_OnlyReceivesNotificationsForTheJoinedWarehouse()
        {
            var joinedWarehouseId = Guid.NewGuid();
            var otherWarehouseId = Guid.NewGuid();

            var token = TestJwtFactory.CreateToken(Guid.NewGuid(), UserRole.Admin);

            await using var connection = BuildConnection(token);

            var received = new TaskCompletionSource<NotificationServiceDTOs.StockLevelChangedNotification>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            connection.On<NotificationServiceDTOs.StockLevelChangedNotification>("StockLevelChanged", notification =>
            {
                received.TrySetResult(notification);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("JoinWarehouse", joinedWarehouseId);

            await PublishStockLevelChangedAsync(otherWarehouseId);

            var completedTooSoon = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.NotSame(received.Task, completedTooSoon);

            await PublishStockLevelChangedAsync(joinedWarehouseId);

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(received.Task, completed);

            var notification = await received.Task;
            Assert.Equal(joinedWarehouseId, notification.WarehouseId);
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
