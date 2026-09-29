using Shared.Kernel.Redis.Tests.Fixtures;
using Xunit;

namespace Shared.Kernel.Redis.Tests
{
    [Collection(RedisCollection.Name)]
    public class RedisDistributedLockTests
    {
        private readonly RedisDistributedLock _distributedLock;

        public RedisDistributedLockTests(RedisContainerFixture redis)
        {
            _distributedLock = new RedisDistributedLock(redis.ConnectionMultiplexer);
        }

        [Fact]
        public async Task TryAcquireAsync_LockFree_ReturnsTrue()
        {
            var key = Guid.NewGuid().ToString();

            Assert.True(await _distributedLock.TryAcquireAsync(key, "instance-1", TimeSpan.FromSeconds(10), CancellationToken.None));
        }

        [Fact]
        public async Task TryAcquireAsync_AlreadyHeldByAnotherInstance_ReturnsFalse()
        {
            var key = Guid.NewGuid().ToString();

            await _distributedLock.TryAcquireAsync(key, "instance-1", TimeSpan.FromSeconds(10), CancellationToken.None);

            Assert.False(await _distributedLock.TryAcquireAsync(key, "instance-2", TimeSpan.FromSeconds(10), CancellationToken.None));
        }

        [Fact]
        public async Task TryAcquireAsync_AfterTtlExpires_CanBeAcquiredAgain()
        {
            var key = Guid.NewGuid().ToString();
            var ttl = TimeSpan.FromMilliseconds(300);

            await _distributedLock.TryAcquireAsync(key, "instance-1", ttl, CancellationToken.None);
            await Task.Delay(ttl + TimeSpan.FromMilliseconds(200));

            Assert.True(await _distributedLock.TryAcquireAsync(key, "instance-2", ttl, CancellationToken.None));
        }

        [Fact]
        public async Task TryAcquireAsync_ConcurrentInstancesRacingForTheSameKey_ExactlyOneWins()
        {
            var key = Guid.NewGuid().ToString();
            const int competingInstances = 20;

            var results = await Task.WhenAll(Enumerable.Range(0, competingInstances)
                .Select(i => _distributedLock.TryAcquireAsync(key, $"instance-{i}", TimeSpan.FromSeconds(10), CancellationToken.None)));

            Assert.Single(results, acquired => acquired);
        }
    }
}
