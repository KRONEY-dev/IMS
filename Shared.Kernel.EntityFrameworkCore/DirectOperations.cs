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

    public class DirectUpdate<TEntity> : IExecutableDirectOperation where TEntity : class
    {
        private readonly Expression<Func<TEntity, bool>> _predicate;
        private readonly Action<UpdateSettersBuilder<TEntity>> _setters;

        public DirectUpdate(Expression<Func<TEntity, bool>> predicate, Action<UpdateSettersBuilder<TEntity>> setters)
        {
            _predicate = predicate;
            _setters = setters;
        }

        public Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            return dbContext.Set<TEntity>().Where(_predicate).ExecuteUpdateAsync(_setters, cancellationToken);
        }
    }

    public class DirectDelete<TEntity> : IExecutableDirectOperation where TEntity : class
    {
        private readonly Expression<Func<TEntity, bool>> _predicate;

        public DirectDelete(Expression<Func<TEntity, bool>> predicate)
        {
            _predicate = predicate;
        }

        public Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            return dbContext.Set<TEntity>().Where(_predicate).ExecuteDeleteAsync(cancellationToken);
        }
    }

    public class DirectInsert<TEntity> : IExecutableDirectOperation where TEntity : class
    {
        private readonly TEntity _entity;

        public DirectInsert(TEntity entity)
        {
            _entity = entity;
        }

        public async Task<int> ExecuteAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            dbContext.Set<TEntity>().Add(_entity);

            return await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}