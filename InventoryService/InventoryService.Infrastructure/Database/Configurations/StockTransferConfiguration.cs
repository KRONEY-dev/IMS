using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
    {
        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.HasKey(stockTransfer => stockTransfer.Id);

            builder.HasIndex(stockTransfer => stockTransfer.ShipmentId);

            builder.HasIndex(stockTransfer => stockTransfer.Status);

            builder.Property(stockTransfer => stockTransfer.Price)
                .HasPrecision(18, 2);
        }
    }
}