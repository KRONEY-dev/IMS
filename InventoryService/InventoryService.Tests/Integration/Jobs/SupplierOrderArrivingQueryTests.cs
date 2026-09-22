using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Infrastructure.Database;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Database;
using Xunit;

namespace InventoryService.Tests.Integration.Jobs
{
    // Proves the real Postgres query behind SupplierOrderReminderJob - not just that the
    // predicate expression compiles, but that it translates into SQL that returns the right
    // rows against a real database.
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class SupplierOrderArrivingQueryTests
    {
        private readonly PostgresContainerFixture _postgres;

        public SupplierOrderArrivingQueryTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [Fact]
        public async Task GetArrivingByAsync_ReturnsOnlySubmittedOrdersDueWithinCutoff()
        {
            var cutoff = DateTime.UtcNow.AddDays(3);

            var withinWindow = await SeedSubmittedOrderAsync(cutoff.AddDays(-1));
            var beyondWindow = await SeedSubmittedOrderAsync(cutoff.AddDays(1));
            var exactlyAtCutoff = await SeedSubmittedOrderAsync(cutoff);
            var alreadyReceived = await SeedSubmittedOrderAsync(cutoff.AddDays(-1));

            await MarkReceivedAsync(alreadyReceived);

            var arrivingIds = await GetArrivingIdsAsync(cutoff);

            Assert.Contains(withinWindow, arrivingIds);
            Assert.DoesNotContain(beyondWindow, arrivingIds);
            Assert.Contains(exactlyAtCutoff, arrivingIds);
            Assert.DoesNotContain(alreadyReceived, arrivingIds);
        }

        private async Task<Guid> SeedSubmittedOrderAsync(DateTime expectedDeliveryDate)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            return await InventorySeeding.SeedSubmittedSupplierOrderAsync(dbContext, expectedDeliveryDate);
        }

        private async Task MarkReceivedAsync(Guid orderId)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = scope.ServiceProvider.GetRequiredService<ISupplierOrderRepository>();

            await unitOfWork.ExecuteInTransactionAsync(repository.BuildReceiveOperation(orderId), CancellationToken.None);
        }

        private async Task<List<Guid>> GetArrivingIdsAsync(DateTime cutoff)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISupplierOrderRepository>();

            var orders = await repository.GetArrivingByAsync(cutoff, CancellationToken.None);

            return orders.Select(order => order.Id).ToList();
        }
    }
}
