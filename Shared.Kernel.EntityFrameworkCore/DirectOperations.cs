using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Shared.Kernel.Database;
using System.Linq.Expressions;

namespace Shared.Kernel.EntityFrameworkCore
{
    public interface IExecutableDirectOperation : IDirectOperation
    {
        Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken);
    }

    public class DirectUpdate<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        Action<UpdateSettersBuilder<TEntity>> setters)
        : IExecutableDirectOperation where TEntity : class
    {
        public Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            return dbContext.Set<TEntity>().Where(predicate).ExecuteUpdateAsync(setters, cancellationToken);
        }
    }

    public class DirectDelete<TEntity>(Expression<Func<TEntity, bool>> predicate)
        : IExecutableDirectOperation where TEntity : class
    {
        public Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            return dbContext.Set<TEntity>().Where(predicate).ExecuteDeleteAsync(cancellationToken);
        }
    }

    public class DirectInsert<TEntity>(TEntity entity)
        : IExecutableDirectOperation where TEntity : class
    {
        public async Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            dbContext.Set<TEntity>().Add(entity);

            return await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}