using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Services.EventHandling;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Infrastructure.Database;
using InventoryService.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Kernel.Database;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Testcontainers.PostgreSql;
using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
    // Real Postgres 17 (matches docker-compose.yml), one container shared across every
    // test in the "Integration" collection. Tests must not depend on table state being
    // empty - each test uses fresh Guids for its own rows instead of resetting the DB.
    public class PostgresContainerFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _container = default!;
        private ServiceProvider _serviceProvider = default!;

        public IServiceScopeFactory ScopeFactory { get; private set; } = default!;
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

            var services = new ServiceCollection();

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(ConnectionString),
                optionsLifetime: ServiceLifetime.Singleton);

            services.AddDbContextFactory<InventoryDbContext>();

            services.AddScoped<IUnitOfWork, UnitOfWorkScoped>();
            services.AddScoped<IStockThresholdRepository, StockThresholdRepositoryScoped>();
            services.AddScoped<IStockItemRepository, StockItemRepositoryScoped>();
            services.AddScoped<ILowStockAlertRepository, LowStockAlertRepositoryScoped>();
            services.AddScoped<IOutboxMessageRepository, OutboxMessageRepositoryScoped>();
            services.AddScoped<ISupplierOrderRepository, SupplierOrderRepositoryScoped>();

            // IRequestContext/IMapperWrapper/INotificationPublisher are irrelevant to the
            // low-stock evaluation logic under test here - loose mocks stand in for them so
            // the real LowStockAlertScopedService/StockQuantityChangedEventHandler chain can
            // still be resolved exactly as RabbitMqConsumerHostedService resolves it in production.
            services.AddSingleton(Mock.Of<IRequestContext>());
            services.AddSingleton(Mock.Of<IMapperWrapper>());
            services.AddSingleton(Mock.Of<INotificationPublisher>());
            services.AddScoped<ILowStockAlertService, LowStockAlertScopedService>();
            services.AddScoped<IInventoryEventHandler, StockQuantityChangedEventHandler>();
            services.AddScoped<IInventoryEventHandlerFactory, InventoryEventHandlerFactory>();

            _serviceProvider = services.BuildServiceProvider();
            ScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _container.DisposeAsync();
        }
    }
}
