using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class SupplierOrderItemConfiguration : IEntityTypeConfiguration<SupplierOrderItem>
    {
        public void Configure(EntityTypeBuilder<SupplierOrderItem> builder)
        {
            builder.HasKey(supplierOrderItem => supplierOrderItem.Id);

            builder.HasOne<SupplierOrder>()
                .WithMany()
                .HasForeignKey(supplierOrderItem => supplierOrderItem.SupplierOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(supplierOrderItem => supplierOrderItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(supplierOrderItem => supplierOrderItem.PurchasePrice)
                .HasPrecision(18, 2);
        }
    }
}