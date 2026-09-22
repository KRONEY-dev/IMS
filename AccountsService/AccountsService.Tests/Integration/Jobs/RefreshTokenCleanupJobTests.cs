using AccountsService.Application.Options;
using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Domain.Entities;
using AccountsService.Infrastructure.Database;
using AccountsService.Infrastructure.Jobs;
using AccountsService.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Kernel.Database;
using Xunit;

namespace AccountsService.Tests.Integration.Jobs
{
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class RefreshTokenCleanupJobTests
    {
        private const int RetentionDays = 30;

        private readonly PostgresContainerFixture _postgres;

        public RefreshTokenCleanupJobTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [Fact]
        public async Task Execute_DeletesOnlyDeadTokensOlderThanRetention()
        {
            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var revokedLongAgo = await SeedTokenAsync(revokedAt: cutoff.AddDays(-1));
            var revokedRecently = await SeedTokenAsync(revokedAt: cutoff.AddDays(1));
            var expiredLongAgo = await SeedTokenAsync(expiresAt: cutoff.AddDays(-1));
            var stillActive = await SeedTokenAsync(expiresAt: DateTime.UtcNow.AddDays(7));

            await RunJobAsync(acquiresLock: true);

            var remainingIds = await GetAllTokenIdsAsync();

            Assert.DoesNotContain(revokedLongAgo, remainingIds);
            Assert.Contains(revokedRecently, remainingIds);
            Assert.DoesNotContain(expiredLongAgo, remainingIds);
            Assert.Contains(stillActive, remainingIds);
        }

        [Fact]
        public async Task Execute_LockNotAcquired_DeletesNothing()
        {
            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
            var revokedLongAgo = await SeedTokenAsync(revokedAt: cutoff.AddDays(-1));

            await RunJobAsync(acquiresLock: false);

            var remainingIds = await GetAllTokenIdsAsync();
            Assert.Contains(revokedLongAgo, remainingIds);
        }

        private async Task<Guid> SeedTokenAsync(DateTime? revokedAt = null, DateTime? expiresAt = null)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();

            var user = new User("Test", "User", null, "+10000000000", UserRole.Worker, "hash");
            dbContext.Users.Add(user);

            var token = RefreshToken.Create(user.Id, Guid.NewGuid(), $"hash-{Guid.NewGuid()}", TimeSpan.FromDays(7));

            // ExpiresAt/RevokedAt have private setters - the only way to backdate them for a
            // boundary test is the same reflection helper the unit tests already use.
            if (expiresAt is not null)
            {
                typeof(RefreshToken).GetProperty(nameof(RefreshToken.ExpiresAt))!.SetValue(token, expiresAt.Value);
            }

            if (revokedAt is not null)
            {
                typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedAt))!.SetValue(token, revokedAt.Value);
            }

            dbContext.RefreshTokens.Add(token);
            await dbContext.SaveChangesAsync();

            return token.Id;
        }

        private async Task RunJobAsync(bool acquiresLock)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            var settings = Options.Create(new RefreshTokenCleanupJobSettings
            {
                CronExpression = "0 0 3 * * ?",
                RetentionDays = RetentionDays,
                LockTtlSeconds = 60
            });

            var job = new RefreshTokenCleanupJob(unitOfWork, repository, new FakeDistributedLock(acquiresLock), settings);

            await job.Execute(null!, CancellationToken.None);
        }

        private async Task<List<Guid>> GetAllTokenIdsAsync()
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();

            return await dbContext.RefreshTokens.Select(token => token.Id).ToListAsync();
        }
    }
}
