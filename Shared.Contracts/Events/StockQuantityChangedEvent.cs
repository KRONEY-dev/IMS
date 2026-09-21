namespace Shared.Contracts.Events
{
    public record StockQuantityChangedEvent(Guid ProductId, Guid WarehouseId, bool Increased);
}
