using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class LowStockAlertConfiguration : IEntityTypeConfiguration<LowStockAlert>
    {
        public void Configure(EntityTypeBuilder<LowStockAlert> builder)
        {
            builder.HasKey(lowStockAlert => lowStockAlert.Id);

            builder.HasIndex(lowStockAlert => new { lowStockAlert.ProductId, lowStockAlert.WarehouseId })
                .IsUnique()
                .HasFilter("\"Status\" = 0");

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(lowStockAlert => lowStockAlert.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(lowStockAlert => lowStockAlert.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}