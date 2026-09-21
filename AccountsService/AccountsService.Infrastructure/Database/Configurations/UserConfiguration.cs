using AccountsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Infrastructure.Database.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(40);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(u => u.WarehouseIds)
               .HasColumnType("uuid[]");

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
        }
    }
}