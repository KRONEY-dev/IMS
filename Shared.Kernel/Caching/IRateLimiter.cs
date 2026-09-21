namespace Shared.Kernel.Caching
{
    public interface IRateLimiter
    {
        Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken);
    }
}
