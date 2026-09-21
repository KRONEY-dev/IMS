namespace InventoryService.Application.Services.EventHandling
{
    public interface IInventoryEventHandler
    {
        string NameKey { get; }

        Task HandleAsync(string payload, CancellationToken cancellationToken);
    }
}
