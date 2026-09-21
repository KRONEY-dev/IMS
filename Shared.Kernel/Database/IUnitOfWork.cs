namespace Shared.Kernel.Database
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken);

        Task<bool> ExecuteInTransactionAsync(IDirectOperation operation, CancellationToken cancellationToken);
        Task<bool> ExecuteInTransactionAsync(IReadOnlyList<IDirectOperation> operations, CancellationToken cancellationToken);
    }
}