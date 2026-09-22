namespace Shared.Kernel.Caching
{
    public interface IUserAccessRevocation
    {
        Task RevokeAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken);

        Task<DateTimeOffset?> GetRevokedAtAsync(Guid userId, CancellationToken cancellationToken);
    }
}
