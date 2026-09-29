using InventoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
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