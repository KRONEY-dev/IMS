using InventoryService.Domain.Entities.Base;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces.Base
{
    public interface IBaseEntityRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}