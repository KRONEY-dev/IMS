using AccountsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Infrastructure.Database.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasIndex(rt => rt.SessionId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<RefreshToken>()
                .WithMany()
                .HasForeignKey(rt => rt.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}