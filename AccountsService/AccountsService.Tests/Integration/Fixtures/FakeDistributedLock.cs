using Shared.Kernel.Caching;

namespace AccountsService.Tests.Integration.Fixtures
{
    public class FakeDistributedLock : IDistributedLock
    {
        private readonly bool _acquires;

        public FakeDistributedLock(bool acquires)
        {
            _acquires = acquires;
        }

        public Task<bool> TryAcquireAsync(string key, string instanceId, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.FromResult(_acquires);
        }
    }
}