namespace Shared.Kernel.Caching
{
    public interface IDistributedLock
    {
        Task<bool> TryAcquireAsync(string key, string instanceId, TimeSpan ttl, CancellationToken cancellationToken);
    }
}
