using AccountsService.Application.Repositories.Interfaces.Base;
using AccountsService.Domain.Entities;
using Shared.Kernel.Database;

namespace AccountsService.Application.Repositories.Interfaces
{
    public interface IRefreshTokenRepository : IBaseEntityRepository<RefreshToken>
    {
        IDirectOperation RevokeChainBySessionId(Guid sessionId);

        IDirectOperation BuildDeleteDeadOlderThanOperation(DateTime cutoff);
    }
}