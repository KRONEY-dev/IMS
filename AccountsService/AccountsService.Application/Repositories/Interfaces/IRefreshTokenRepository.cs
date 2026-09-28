using AccountsService.Application.Repositories.Interfaces.Base;
using AccountsService.Domain.Entities;
using Shared.Kernel.Database;

namespace AccountsService.Application.Repositories.Interfaces
{
    public interface IRefreshTokenRepository : IBaseEntityRepository<RefreshToken>
    {
        IDirectOperation RevokeChainBySessionId(Guid sessionId);

        IDirectOperation BuildRotateOperation(Guid tokenId, Guid replacedByTokenId);

        IDirectOperation BuildCreateOperation(RefreshToken token);

        IDirectOperation BuildDeleteDeadOlderThanOperation(DateTime cutoff);
    }
}