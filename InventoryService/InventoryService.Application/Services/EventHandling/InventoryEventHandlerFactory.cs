namespace InventoryService.Application.Services.EventHandling
{
    public class InventoryEventHandlerFactory : IInventoryEventHandlerFactory
    {
        private readonly IReadOnlyDictionary<string, IInventoryEventHandler> _handlersByNameKey;

        public InventoryEventHandlerFactory(IEnumerable<IInventoryEventHandler> handlers)
        {
            _handlersByNameKey = handlers.ToDictionary(handler => handler.NameKey);
        }

        public IInventoryEventHandler? GetHandler(string nameKey)
        {
            return _handlersByNameKey.GetValueOrDefault(nameKey);
        }
    }
}
