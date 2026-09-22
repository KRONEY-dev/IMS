using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class StockThresholdConfiguration : IEntityTypeConfiguration<StockThreshold>
    {
        public void Configure(EntityTypeBuilder<StockThreshold> builder)
        {
            builder.HasKey(stockThreshold => stockThreshold.Id);

            builder.HasIndex(stockThreshold => new { stockThreshold.ProductId, stockThreshold.WarehouseId })
                .IsUnique();

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(stockThreshold => stockThreshold.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(stockThreshold => stockThreshold.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // UseXminAsConcurrencyToken() was removed from newer versions of the Npgsql provider —
            // map the shadow property directly onto Postgres's system column "xmin" (type xid) instead.
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        }
    }
}