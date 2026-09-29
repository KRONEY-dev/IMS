using AccountsService.Application.Options;
using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Application.Services;
using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using AccountsService.Domain.Entities;
using AccountsService.Domain.Exceptions;
using AccountsService.Infrastructure.Database;
using AccountsService.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Kernel.Caching;
using Shared.Kernel.Database;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Xunit;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Tests.Integration.Services
{
    // AuthScopedServiceTests (unit, mocked IUnitOfWork) can only prove that the service calls
    // ExecuteInTransactionAsync with the right operations - it cannot prove the conditional UPDATE
    // those operations compile to is actually atomic under real concurrent requests against real
    // Postgres. This is the same class of gap that produced two real bugs in this rotation logic
    // earlier - this test races two genuinely concurrent refreshes of the same token against the
    // real database instead of trusting that the SQL predicate does what it says.
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class AuthScopedServiceConcurrencyTests
    {
        private readonly PostgresContainerFixture _postgres;

        public AuthScopedServiceConcurrencyTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_TwoConcurrentRequestsForTheSameToken_ExactlyOneWinsAndTheLoserDoesNotDestroyItsSession()
        {
            const string rawSecret = "concurrent-raw-secret";
            var (user, token) = await SeedUserAndTokenAsync(rawSecret);

            var request = new AuthServiceDTOs.RefreshAccessTokenRequestDTO(new AuthServiceDTOs.RefreshTokenDTO(token.Id, rawSecret));

            await using var scopeA = _postgres.ScopeFactory.CreateAsyncScope();
            await using var scopeB = _postgres.ScopeFactory.CreateAsyncScope();

            var serviceA = BuildAuthScopedService(scopeA);
            var serviceB = BuildAuthScopedService(scopeB);

            var taskA = InvokeAsync(serviceA, request);
            var taskB = InvokeAsync(serviceB, request);

            var results = await Task.WhenAll(taskA, taskB);

            var successes = results.Count(r => r.Response is not null);
            var softFailures = results.Count(r => r.Exception is InvalidRefreshTokenException);

            // Within the grace period, losing the race is indistinguishable from a benign client
            // retry - it must fail softly (InvalidRefreshTokenException), never
            // RefreshTokenReuseDetectedException, which would revoke the winner's brand-new session
            // for a race the winner did nothing wrong in.
            Assert.Equal(1, successes);
            Assert.Equal(1, softFailures);

            var winnerNewTokenId = results.Single(r => r.Response is not null).Response!.RefreshToken.Id;

            await using var verifyScope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<AccountsDbContext>();
            var originalToken = await dbContext.RefreshTokens.FindAsync(token.Id);
            var winnerNewToken = await dbContext.RefreshTokens.FindAsync(winnerNewTokenId);

            // The original token is revoked by the winner's own successful rotation - not by a
            // session-wide chain revoke.
            Assert.NotNull(originalToken!.RevokedAt);

            // The winner's brand-new token must stay usable - the loser must not have nuked it.
            Assert.Null(winnerNewToken!.RevokedAt);
        }

        private async Task<(AuthServiceDTOs.RefreshAccessTokenResponseDTO? Response, Exception? Exception)> InvokeAsync(
            IAuthService service, AuthServiceDTOs.RefreshAccessTokenRequestDTO request)
        {
            try
            {
                var response = await service.RefreshAccessTokenAsync(request, CancellationToken.None);
                return (response, null);
            }
            catch (Exception exception)
            {
                return (null, exception);
            }
        }

        private static AuthScopedService BuildAuthScopedService(AsyncServiceScope scope)
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var tokenSignerMock = new Mock<ITokenSignerService>();
            tokenSignerMock.Setup(s => s.HashRefreshSecret(It.IsAny<string>())).Returns((string raw) => $"hash-{raw}");
            tokenSignerMock.Setup(s => s.GenerateRefreshSecret()).Returns(() =>
            {
                var raw = Guid.NewGuid().ToString();
                return (raw, $"hash-{raw}");
            });
            tokenSignerMock.Setup(s => s.SignAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), It.IsAny<TimeSpan>()))
                .Returns("dummy-access-token");

            var jwtSettings = Options.Create(new JwtSettings
            {
                Issuer = "ims-accounts-tests",
                Audience = "ims-tests",
                AccessTokenLifetime = TimeSpan.FromMinutes(15),
                RefreshTokenLifetime = TimeSpan.FromDays(7),
                RefreshTokenReuseGracePeriod = TimeSpan.FromSeconds(5),
                PrivateKeyPath = "unused-in-tests.pem"
            });

            return new AuthScopedService(unitOfWork, Mock.Of<IRequestContext>(), Mock.Of<IMapperWrapper>(),
                refreshTokenRepository, userRepository, tokenSignerMock.Object,
                Mock.Of<IPasswordHasherService>(), Mock.Of<IAccessTokenBlacklist>(), jwtSettings);
        }

        private async Task<(User User, RefreshToken Token)> SeedUserAndTokenAsync(string rawSecret)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();

            var user = new User("Test", "User", null, null, UserRole.Worker, "password-hash");
            dbContext.Users.Add(user);

            var token = RefreshToken.Create(user.Id, Guid.NewGuid(), $"hash-{rawSecret}", TimeSpan.FromDays(7));
            dbContext.RefreshTokens.Add(token);

            await dbContext.SaveChangesAsync();

            return (user, token);
        }
    }
}
