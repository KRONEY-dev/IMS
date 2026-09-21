using AccountsService.Application.Repositories.Interfaces.Base;
using AccountsService.Domain.Entities;
using Shared.Kernel.Database;

namespace AccountsService.Application.Repositories.Interfaces
{
    public interface IUserRepository : IBaseEntityRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

        Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);
        Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);
    }
}