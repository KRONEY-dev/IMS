using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class SupplierOrderConfiguration : IEntityTypeConfiguration<SupplierOrder>
    {
        public void Configure(EntityTypeBuilder<SupplierOrder> builder)
        {
            builder.HasKey(supplierOrder => supplierOrder.Id);

            builder.HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(supplierOrder => supplierOrder.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(supplierOrder => supplierOrder.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}