using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Infrastructure.Database;
using AccountsService.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Database;
using Testcontainers.PostgreSql;
using Xunit;

namespace AccountsService.Tests.Integration.Fixtures
{
    // Real Postgres 17 (matches docker-compose.yml), one container shared across every
    // test in the "Integration" collection. Tests must not depend on table state being
    // empty - each test uses fresh Guids for its own rows instead of resetting the DB.
    public class PostgresContainerFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _container = default!;
        private ServiceProvider _serviceProvider = default!;

        public IServiceScopeFactory ScopeFactory { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            _container = new PostgreSqlBuilder("postgres:17")
                .WithDatabase("accounts")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            var services = new ServiceCollection();

            services.AddDbContext<AccountsDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString()),
                optionsLifetime: ServiceLifetime.Singleton);

            services.AddDbContextFactory<AccountsDbContext>();

            services.AddScoped<IUnitOfWork, UnitOfWorkScoped>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepositoryScoped>();

            _serviceProvider = services.BuildServiceProvider();
            ScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _container.DisposeAsync();
        }
    }
}
