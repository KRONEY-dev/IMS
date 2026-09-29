using Shared.Kernel.Redis.Tests.Fixtures;
using Xunit;

namespace Shared.Kernel.Redis.Tests
{
    [Collection(RedisCollection.Name)]
    public class RedisRateLimiterTests
    {
        private readonly RedisRateLimiter _rateLimiter;

        public RedisRateLimiterTests(RedisContainerFixture redis)
        {
            _rateLimiter = new RedisRateLimiter(redis.ConnectionMultiplexer);
        }

        [Fact]
        public async Task TryAcquireAsync_UnderLimit_ReturnsTrue()
        {
            var key = Guid.NewGuid().ToString();

            for (var i = 0; i < 3; i++)
            {
                Assert.True(await _rateLimiter.TryAcquireAsync(key, limit: 3, TimeSpan.FromSeconds(10), CancellationToken.None));
            }
        }

        [Fact]
        public async Task TryAcquireAsync_ExceedsLimit_ReturnsFalse()
        {
            var key = Guid.NewGuid().ToString();

            for (var i = 0; i < 3; i++)
            {
                await _rateLimiter.TryAcquireAsync(key, limit: 3, TimeSpan.FromSeconds(10), CancellationToken.None);
            }

            Assert.False(await _rateLimiter.TryAcquireAsync(key, limit: 3, TimeSpan.FromSeconds(10), CancellationToken.None));
        }

        [Fact]
        public async Task TryAcquireAsync_AfterWindowExpires_ResetsCount()
        {
            var key = Guid.NewGuid().ToString();
            var window = TimeSpan.FromMilliseconds(300);

            await _rateLimiter.TryAcquireAsync(key, limit: 1, window, CancellationToken.None);
            Assert.False(await _rateLimiter.TryAcquireAsync(key, limit: 1, window, CancellationToken.None));

            await Task.Delay(window + TimeSpan.FromMilliseconds(200));

            Assert.True(await _rateLimiter.TryAcquireAsync(key, limit: 1, window, CancellationToken.None));
        }

        [Fact]
        public async Task TryAcquireAsync_ConcurrentRequestsAtTheLimitBoundary_NeverAllowsMoreThanTheLimit()
        {
            var key = Guid.NewGuid().ToString();
            const int limit = 10;
            const int concurrentRequests = 30;

            var results = await Task.WhenAll(Enumerable.Range(0, concurrentRequests)
                .Select(_ => _rateLimiter.TryAcquireAsync(key, limit, TimeSpan.FromSeconds(10), CancellationToken.None)));

            Assert.Equal(limit, results.Count(allowed => allowed));
        }
    }
}