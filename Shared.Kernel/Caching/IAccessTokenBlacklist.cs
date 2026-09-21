namespace Shared.Kernel.Caching
{
    public interface IAccessTokenBlacklist
    {
        Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken);
        Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken);
    }
}