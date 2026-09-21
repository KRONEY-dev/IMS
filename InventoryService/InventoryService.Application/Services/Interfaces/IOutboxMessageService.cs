using Shared.Kernel.Database;

namespace InventoryService.Application.Services.Interfaces
{
    public interface IOutboxMessageService
    {
        void Add<TEvent>(TEvent eventData);

        IDirectOperation BuildCreateOperation<TEvent>(TEvent eventData);
    }
}