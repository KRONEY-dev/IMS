using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
    {
        public void Configure(EntityTypeBuilder<StockItem> builder)
        {
            builder.HasKey(stockItem => stockItem.Id);

            builder.HasIndex(stockItem => new { stockItem.WarehouseId, stockItem.ProductId, stockItem.BatchId })
                .IsUnique();

            builder.Property(stockItem => stockItem.Price)
                .HasPrecision(18, 2);

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(stockItem => stockItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(stockItem => stockItem.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // UseXminAsConcurrencyToken() was removed from newer versions of the Npgsql provider —
            // map the shadow property directly onto Postgres's system column "xmin" (type xid) instead.
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        }
    }
}