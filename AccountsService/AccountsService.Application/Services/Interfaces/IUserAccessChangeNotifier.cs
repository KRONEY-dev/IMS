namespace AccountsService.Application.Services.Interfaces
{
    public interface IUserAccessChangeNotifier
    {
        Task NotifyWarehouseChangedAsync(Guid userId, Guid warehouseId, bool added, CancellationToken cancellationToken);

        Task NotifyAccessRevokedAsync(Guid userId, CancellationToken cancellationToken);
    }
}
