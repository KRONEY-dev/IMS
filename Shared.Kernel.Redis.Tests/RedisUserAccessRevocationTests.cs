using Shared.Kernel.Redis.Tests.Fixtures;
using Xunit;

namespace Shared.Kernel.Redis.Tests
{
    [Collection(RedisCollection.Name)]
    public class RedisUserAccessRevocationTests
    {
        private readonly RedisUserAccessRevocation _revocation;

        public RedisUserAccessRevocationTests(RedisContainerFixture redis)
        {
            _revocation = new RedisUserAccessRevocation(redis.ConnectionMultiplexer);
        }

        [Fact]
        public async Task GetRevokedAtAsync_NeverRevoked_ReturnsNull()
        {
            var userId = Guid.NewGuid();

            Assert.Null(await _revocation.GetRevokedAtAsync(userId, CancellationToken.None));
        }

        [Fact]
        public async Task GetRevokedAtAsync_AfterRevoking_ReturnsTheRevocationTime()
        {
            var userId = Guid.NewGuid();
            var before = DateTimeOffset.UtcNow;

            await _revocation.RevokeAsync(userId, TimeSpan.FromSeconds(10), CancellationToken.None);

            var revokedAt = await _revocation.GetRevokedAtAsync(userId, CancellationToken.None);

            Assert.NotNull(revokedAt);
            Assert.InRange(revokedAt.Value, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task GetRevokedAtAsync_AfterTtlExpires_ReturnsNull()
        {
            var userId = Guid.NewGuid();
            var ttl = TimeSpan.FromMilliseconds(300);

            await _revocation.RevokeAsync(userId, ttl, CancellationToken.None);
            await Task.Delay(ttl + TimeSpan.FromMilliseconds(200));

            Assert.Null(await _revocation.GetRevokedAtAsync(userId, CancellationToken.None));
        }
    }
}