using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IOutboxMessageRepository : IBaseEntityRepository<OutboxMessage>
    {
        Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken);

        IDirectOperation BuildCreateOperation(OutboxMessage message);

        IDirectOperation BuildMarkProcessedOperation(Guid messageId);
    }
}
