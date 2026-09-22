using Shared.Kernel.Caching;

namespace AccountsService.Tests.Integration.Fixtures
{
    // Stands in for the real Redis-backed IDistributedLock so job tests can exercise the
    // job's own logic against a real database without also needing a Redis container.
    public class FakeDistributedLock(bool acquires) : IDistributedLock
    {
        public Task<bool> TryAcquireAsync(string key, string instanceId, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.FromResult(acquires);
        }
    }
}
