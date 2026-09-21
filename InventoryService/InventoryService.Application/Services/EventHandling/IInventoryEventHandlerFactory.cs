namespace InventoryService.Application.Services.EventHandling
{
    public interface IInventoryEventHandlerFactory
    {
        IInventoryEventHandler? GetHandler(string nameKey);
    }
}
