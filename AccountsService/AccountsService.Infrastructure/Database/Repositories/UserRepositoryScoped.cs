using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Domain.Entities;
using AccountsService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.EntityFrameworkCore;

namespace AccountsService.Infrastructure.Database.Repositories
{
    public class UserRepositoryScoped(AccountsDbContext dbContext) :
        BaseEntityRepository<User, AccountsDbContext>(dbContext), IUserRepository
    {
        protected override DbSet<User> MainTable => DbContext.Users;

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLowerInvariant();

            return MainTable.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLowerInvariant();

            return MainTable.AnyAsync(u => u.Email == normalized, cancellationToken);
        }

        public Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            var normalized = phoneNumber.Trim();

            return MainTable.FirstOrDefaultAsync(u => u.PhoneNumber == normalized, cancellationToken);
        }

        public Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            var normalized = phoneNumber.Trim();

            return MainTable.AnyAsync(u => u.PhoneNumber == normalized, cancellationToken);
        }
    }
}