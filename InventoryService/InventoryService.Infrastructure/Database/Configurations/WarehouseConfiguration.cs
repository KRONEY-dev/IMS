using InventoryService.Domain.Entities;
using InventoryService.Domain.Entities.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Database.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.HasKey(warehouse => warehouse.Id);

            builder.Property(warehouse => warehouse.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(warehouse => warehouse.Name)
                .IsUnique();

            builder.Property(warehouse => warehouse.Location)
                .IsRequired();

            builder.OwnsMany(warehouse => warehouse.WorkingHours, workingHours =>
            {
                workingHours.WithOwner().HasForeignKey("WarehouseId");
                workingHours.HasKey("WarehouseId", nameof(WorkingHours.DayOfWeek));
                workingHours.ToTable("WarehouseWorkingHours");
            });
        }
    }
}