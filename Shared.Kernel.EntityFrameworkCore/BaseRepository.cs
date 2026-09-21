using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;

namespace Shared.Kernel.EntityFrameworkCore
{
    public abstract class BaseRepository<TEntity, TDbContext> : IBaseRepository<TEntity>
        where TEntity : class
        where TDbContext : DbContext
    {
        protected readonly TDbContext DbContext;

        protected abstract DbSet<TEntity> MainTable { get; }

        public BaseRepository(TDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public virtual void Add(TEntity entity)
        {
            MainTable.Add(entity);
        }

        public virtual void Remove(TEntity entity)
        {
            MainTable.Remove(entity);
        }

        public virtual Task ReloadAsync(TEntity entity, CancellationToken cancellationToken)
        {
            return DbContext.Entry(entity).ReloadAsync(cancellationToken);
        }
    }
}