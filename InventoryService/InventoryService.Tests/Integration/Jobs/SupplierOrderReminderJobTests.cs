using InventoryService.Application.Options;
using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Infrastructure.Database;
using InventoryService.Infrastructure.Jobs;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryService.Tests.Integration.Jobs
{
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class SupplierOrderReminderJobTests
    {
        private readonly PostgresContainerFixture _postgres;

        public SupplierOrderReminderJobTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [Fact]
        public async Task Execute_LogsOnlyOrdersArrivingWithinLookaheadWindow()
        {
            var lookaheadDays = 3;

            Guid arrivingOrderId;
            Guid distantOrderId;

            await using (var scope = _postgres.ScopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                arrivingOrderId = await InventorySeeding.SeedSubmittedSupplierOrderAsync(
                    dbContext, DateTime.UtcNow.AddDays(lookaheadDays - 1));
                distantOrderId = await InventorySeeding.SeedSubmittedSupplierOrderAsync(
                    dbContext, DateTime.UtcNow.AddDays(lookaheadDays + 10));
            }

            var logger = new CapturingLogger<SupplierOrderReminderJob>();

            await using (var scope = _postgres.ScopeFactory.CreateAsyncScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<ISupplierOrderRepository>();

                var settings = Options.Create(new SupplierOrderReminderJobSettings
                {
                    CronExpression = "0 0 8 * * ?",
                    LookaheadDays = lookaheadDays,
                    LockTtlSeconds = 60
                });

                var job = new SupplierOrderReminderJob(repository, new FakeDistributedLock(acquires: true), settings, logger);

                await job.Execute(null!, CancellationToken.None);
            }

            Assert.Contains(logger.Messages, message => message.Contains(arrivingOrderId.ToString()));
            Assert.DoesNotContain(logger.Messages, message => message.Contains(distantOrderId.ToString()));
        }

        [Fact]
        public async Task Execute_LockNotAcquired_LogsNothing()
        {
            await using (var scope = _postgres.ScopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await InventorySeeding.SeedSubmittedSupplierOrderAsync(dbContext, DateTime.UtcNow.AddDays(1));
            }

            var logger = new CapturingLogger<SupplierOrderReminderJob>();

            await using (var scope = _postgres.ScopeFactory.CreateAsyncScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<ISupplierOrderRepository>();

                var settings = Options.Create(new SupplierOrderReminderJobSettings
                {
                    CronExpression = "0 0 8 * * ?",
                    LookaheadDays = 3,
                    LockTtlSeconds = 60
                });

                var job = new SupplierOrderReminderJob(repository, new FakeDistributedLock(acquires: false), settings, logger);

                await job.Execute(null!, CancellationToken.None);
            }

            Assert.Empty(logger.Messages);
        }
    }
}
