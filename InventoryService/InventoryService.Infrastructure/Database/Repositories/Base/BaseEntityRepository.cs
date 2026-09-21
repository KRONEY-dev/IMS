using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories.Base
{
    public abstract class BaseEntityRepository<TEntity, TDbContext> :
        BaseRepository<TEntity, TDbContext>, IBaseEntityRepository<TEntity>
        where TEntity : BaseEntity
        where TDbContext : DbContext
    {
        protected BaseEntityRepository(TDbContext dbContext) : base(dbContext) { }

        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return MainTable.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var entity = await GetByIdAsync(id, cancellationToken);

            if (entity is null)
            {
                return false;
            }

            Remove(entity);
            return true;
        }
    }
}