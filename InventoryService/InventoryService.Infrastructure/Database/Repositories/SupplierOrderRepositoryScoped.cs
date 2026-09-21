using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class SupplierOrderRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<SupplierOrder, InventoryDbContext>(dbContext), ISupplierOrderRepository
    {
        protected override DbSet<SupplierOrder> MainTable => DbContext.SupplierOrders;

        public Task<List<SupplierOrder>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }

        public Task<List<SupplierOrder>> GetArrivingByAsync(DateTime cutoff, CancellationToken cancellationToken)
        {
            return MainTable.Where(SupplierOrder.IsArrivingBy(cutoff)).ToListAsync(cancellationToken);
        }

        public IDirectOperation BuildReceiveOperation(Guid orderId)
        {
            var transition = SupplierOrder.ReceiveTransition();

            return new DirectUpdate<SupplierOrder>(
                SupplierOrder.IsSubmitted(orderId),
                setters => setters
                    .SetProperty(order => order.Status, transition.Status)
                    .SetProperty(order => order.ReceivedAt, transition.ReceivedAt));
        }
    }
}