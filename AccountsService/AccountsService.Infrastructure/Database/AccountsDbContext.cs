using AccountsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountsService.Infrastructure.Database
{
    public class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users
        {
            get
            {
                return Set<User>();
            }
        }

        public DbSet<RefreshToken> RefreshTokens
        {
            get
            {
                return Set<RefreshToken>();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);
        }
    }
}