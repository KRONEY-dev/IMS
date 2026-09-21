namespace Shared.Kernel.Database
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
        void Add(TEntity entity);
        void Remove(TEntity entity);

        Task ReloadAsync(TEntity entity, CancellationToken cancellationToken);
    }
}