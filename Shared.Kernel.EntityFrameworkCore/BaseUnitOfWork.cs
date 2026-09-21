using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Npgsql;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;

namespace Shared.Kernel.EntityFrameworkCore
{
    public abstract class BaseUnitOfWork<TDbContext> : IUnitOfWork where TDbContext : DbContext
    {
        protected readonly TDbContext DbContext;
        protected readonly IDbContextFactory<TDbContext> DbContextFactory;

        public BaseUnitOfWork(TDbContext dbContext, IDbContextFactory<TDbContext> dbContextFactory)
        {
            DbContext = dbContext;
            DbContextFactory = dbContextFactory;
        }

        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConcurrencyConflictException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                throw new UniqueConstraintViolationException(exception);
            }
            catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
            {
                throw new ReferencedEntityInUseException(exception);
            }
        }

        public virtual Task<bool> ExecuteInTransactionAsync(IDirectOperation operation, CancellationToken cancellationToken)
        {
            return ExecuteInTransactionAsync([operation], cancellationToken);
        }

        public virtual async Task<bool> ExecuteInTransactionAsync(IReadOnlyList<IDirectOperation> operations, CancellationToken cancellationToken)
        {
            await using var isolatedContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await isolatedContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var operation in operations)
                {
                    if (operation is not IExecutableDirectOperation executable)
                    {
                        throw new InvalidOperationException($"Unknown direct operation type '{operation.GetType().Name}'.");
                    }

                    var affectedRows = await executable.ExecuteAsync(isolatedContext, cancellationToken);

                    if (affectedRows == 0)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return false;
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new UniqueConstraintViolationException(exception);
            }
            catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ReferencedEntityInUseException(exception);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
        }

        private static bool IsForeignKeyViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
        }
    }
}