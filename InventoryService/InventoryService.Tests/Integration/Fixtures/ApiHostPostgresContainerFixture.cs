using InventoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
    // A Postgres container dedicated to InventoryApiFactory's real app host - deliberately
    // separate from PostgresContainerFixture. The real host runs its own long-lived
    // RabbitMqOutboxPublisherHostedService/RabbitMqConsumerHostedService for as long as the
    // collection is alive; if it shared PostgresContainerFixture's database, it would compete
    // with OutboxPublisherIdempotencyTests/ConsumerDuplicateDeliveryTests for the same
    // OutboxMessages rows and silently steal them onto its own queue before those tests'
    // own hosted-service instances get a chance to process them.
    public class ApiHostPostgresContainerFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _container = default!;

        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            _container = new PostgreSqlBuilder("postgres:17")
                .WithDatabase("inventory")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();

            await using var dbContext = CreateDbContext();
            await dbContext.Database.MigrateAsync();
        }

        public InventoryDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(ConnectionString).Options;

            return new InventoryDbContext(options);
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }
    }
}
