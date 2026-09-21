using Microsoft.EntityFrameworkCore;
using Shared.Kernel.EntityFrameworkCore;

namespace AccountsService.Infrastructure.Database
{
    public class UnitOfWorkScoped(AccountsDbContext dbContext, IDbContextFactory<AccountsDbContext> dbContextFactory) :
        BaseUnitOfWork<AccountsDbContext>(dbContext, dbContextFactory)
    {

    }
}