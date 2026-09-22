namespace InventoryService.Infrastructure.RealTime
{
    public interface IUserConnectionRegistry
    {
        Task AddConnectionAsync(Guid userId, string connectionId, CancellationToken cancellationToken);

        Task RemoveConnectionAsync(Guid userId, string connectionId, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> GetConnectionIdsAsync(Guid userId, CancellationToken cancellationToken);
    }
}
