namespace Shared.Contracts.Events
{
    public record UserWarehouseAccessChangedEvent(Guid UserId, Guid WarehouseId, bool Added);
}
