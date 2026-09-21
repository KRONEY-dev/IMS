using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.HasKey(stockMovement => stockMovement.Id);

            builder.HasIndex(stockMovement => stockMovement.StockItemId);
            builder.HasIndex(stockMovement => stockMovement.Reference);

            builder.Property(stockMovement => stockMovement.Price)
                .HasPrecision(18, 2);
        }
    }
}