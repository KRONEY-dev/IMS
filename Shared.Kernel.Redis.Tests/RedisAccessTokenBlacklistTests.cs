using Shared.Kernel.Redis.Tests.Fixtures;
using Xunit;

namespace Shared.Kernel.Redis.Tests
{
    [Collection(RedisCollection.Name)]
    public class RedisAccessTokenBlacklistTests
    {
        private readonly RedisAccessTokenBlacklist _blacklist;

        public RedisAccessTokenBlacklistTests(RedisContainerFixture redis)
        {
            _blacklist = new RedisAccessTokenBlacklist(redis.ConnectionMultiplexer);
        }

        [Fact]
        public async Task IsBlacklistedAsync_JtiNeverBlacklisted_ReturnsFalse()
        {
            var jti = Guid.NewGuid().ToString();

            Assert.False(await _blacklist.IsBlacklistedAsync(jti, CancellationToken.None));
        }

        [Fact]
        public async Task IsBlacklistedAsync_AfterBlacklisting_ReturnsTrue()
        {
            var jti = Guid.NewGuid().ToString();

            await _blacklist.BlacklistAsync(jti, TimeSpan.FromSeconds(10), CancellationToken.None);

            Assert.True(await _blacklist.IsBlacklistedAsync(jti, CancellationToken.None));
        }

        [Fact]
        public async Task IsBlacklistedAsync_AfterTtlExpires_ReturnsFalse()
        {
            var jti = Guid.NewGuid().ToString();
            var ttl = TimeSpan.FromMilliseconds(300);

            await _blacklist.BlacklistAsync(jti, ttl, CancellationToken.None);
            await Task.Delay(ttl + TimeSpan.FromMilliseconds(200));

            Assert.False(await _blacklist.IsBlacklistedAsync(jti, CancellationToken.None));
        }
    }
}