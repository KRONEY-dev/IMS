using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Enums;
using InventoryService.Infrastructure.Database;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace InventoryService.Tests.Integration
{
    // The full stock-sale pipeline end to end, against the real app host and real
    // Postgres/RabbitMQ/Redis: a real Sell HTTP call decrements stock and writes an outbox row,
    // the real outbox publisher moves it onto a real queue, the real consumer re-evaluates the
    // threshold and creates a LowStockAlert, and both hops (the immediate StockLevelChanged push
    // and the delayed, async LowStockAlert push) land on a real SignalR client connection.
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class FullPipelineE2ETests
    {
        private readonly ApiHostPostgresContainerFixture _postgres;
        private readonly InventoryApiFactory _factory;

        public FullPipelineE2ETests(
            ApiHostPostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq, RedisContainerFixture redis, InventoryApiFactory factory)
        {
            factory.PostgresConnectionString = postgres.ConnectionString;
            factory.RedisConnectionString = redis.ConnectionString;
            factory.RabbitMqHostName = rabbitMq.Hostname;
            factory.RabbitMqPort = rabbitMq.Port;

            _postgres = postgres;
            _factory = factory;
        }

        [Fact]
        public async Task Sell_BelowThreshold_PushesStockLevelChangedThenLowStockAlertOverSignalR()
        {
            try
            {
                var (productId, warehouseId) = await SeedProductAndWarehouseAsync();
                var token = TestJwtFactory.CreateToken(Guid.NewGuid(), UserRole.Admin);

                using var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                await using var connection = BuildConnection(token);

                var stockLevelChanged = new TaskCompletionSource<NotificationServiceDTOs.StockLevelChangedNotification>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                var lowStockAlert = new TaskCompletionSource<NotificationServiceDTOs.LowStockAlertNotification>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                connection.On<NotificationServiceDTOs.StockLevelChangedNotification>(
                    "StockLevelChanged", notification => stockLevelChanged.TrySetResult(notification));
                connection.On<NotificationServiceDTOs.LowStockAlertNotification>(
                    "LowStockAlert", notification => lowStockAlert.TrySetResult(notification));

                await connection.StartAsync();
                await connection.InvokeAsync("JoinWarehouse", warehouseId);

                var thresholdResponse = await client.PostAsJsonAsync("api/Stock/CreateThreshold",
                    new StockServiceDTOs.CreateStockThresholdRequestDTO(productId, warehouseId, ReorderLevel: 10, ReorderQuantity: 5));
                thresholdResponse.EnsureSuccessStatusCode();

                var receiveResponse = await client.PostAsJsonAsync("api/Stock/Receive",
                    new StockServiceDTOs.ReceiveStockRequestDTO(productId, warehouseId, Quantity: 20, Price: 1m));
                receiveResponse.EnsureSuccessStatusCode();
                var stockItem = await receiveResponse.Content.ReadFromJsonAsync<StockServiceDTOs.StockItemDTO>();

                // 20 -> 5 crosses below the reorder level of 10.
                var sellResponse = await client.PostAsJsonAsync("api/Stock/Sell",
                    new StockServiceDTOs.SellStockRequestDTO(stockItem!.Id, Quantity: 15));
                sellResponse.EnsureSuccessStatusCode();

                var stockLevelCompleted = await Task.WhenAny(stockLevelChanged.Task, Task.Delay(TimeSpan.FromSeconds(5)));
                Assert.Same(stockLevelChanged.Task, stockLevelCompleted);

                // Unlike StockLevelChanged (pushed synchronously inside the Sell request), LowStockAlert
                // only arrives after the outbox publisher and consumer hop through a real RabbitMQ queue.
                var lowStockCompleted = await Task.WhenAny(lowStockAlert.Task, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(lowStockAlert.Task, lowStockCompleted);

                var alertNotification = await lowStockAlert.Task;
                Assert.Equal(productId, alertNotification.ProductId);
                Assert.Equal(warehouseId, alertNotification.WarehouseId);

                await AssertActiveAlertPersistedAsync(productId, warehouseId);
            }
            finally
            {
                // The host's response body never carries the real exception for a 500 by design -
                // this is the only reliable way to see what the server actually threw.
                foreach (var entry in _factory.Logs.Entries)
                {
                    Console.WriteLine(entry);
                }
            }
        }

        private async Task<(Guid ProductId, Guid WarehouseId)> SeedProductAndWarehouseAsync()
        {
            await using var dbContext = _postgres.CreateDbContext();

            return await InventorySeeding.SeedProductAndWarehouseAsync(dbContext);
        }

        private async Task AssertActiveAlertPersistedAsync(Guid productId, Guid warehouseId)
        {
            await using var dbContext = _postgres.CreateDbContext();

            var hasActiveAlert = await dbContext.LowStockAlerts.AnyAsync(alert =>
                alert.ProductId == productId && alert.WarehouseId == warehouseId
                && alert.Status == InventoryService.Domain.Entities.LowStockAlertStatus.Active);

            Assert.True(hasActiveAlert);
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
