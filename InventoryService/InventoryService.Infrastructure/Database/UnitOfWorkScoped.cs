using Microsoft.EntityFrameworkCore;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database
{
    public class UnitOfWorkScoped(InventoryDbContext dbContext, IDbContextFactory<InventoryDbContext> dbContextFactory) :
        BaseUnitOfWork<InventoryDbContext>(dbContext, dbContextFactory)
    {

    }
}