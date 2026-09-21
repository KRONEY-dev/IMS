using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class OutboxMessageRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<OutboxMessage, InventoryDbContext>(dbContext), IOutboxMessageRepository
    {
        protected override DbSet<OutboxMessage> MainTable => DbContext.OutboxMessages;

        public Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken)
        {
            return MainTable
                .Where(OutboxMessage.IsUnprocessed())
                .OrderBy(message => message.OccurredAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public IDirectOperation BuildCreateOperation(OutboxMessage message)
        {
            return new DirectInsert<OutboxMessage>(message);
        }

        public IDirectOperation BuildMarkProcessedOperation(Guid messageId)
        {
            return new DirectUpdate<OutboxMessage>(
                OutboxMessage.IsUnprocessed(messageId),
                setters => setters.SetProperty(message => message.ProcessedAt, DateTime.UtcNow));
        }
    }
}
