using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Services
{
    public class OutboxMessageScopedService : IOutboxMessageService
    {
        private readonly IOutboxMessageRepository _outboxMessageRepository;

        public OutboxMessageScopedService(IOutboxMessageRepository outboxMessageRepository)
        {
            _outboxMessageRepository = outboxMessageRepository;
        }

        public void Add<TEvent>(TEvent eventData)
        {
            _outboxMessageRepository.Add(OutboxMessage.CreateFor(eventData));
        }

        public IDirectOperation BuildCreateOperation<TEvent>(TEvent eventData)
        {
            return _outboxMessageRepository.BuildCreateOperation(OutboxMessage.CreateFor(eventData));
        }
    }
}