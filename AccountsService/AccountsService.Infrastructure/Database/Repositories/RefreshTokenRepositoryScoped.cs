using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Domain.Entities;
using AccountsService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace AccountsService.Infrastructure.Database.Repositories
{
    public class RefreshTokenRepositoryScoped(AccountsDbContext dbContext) :
        BaseEntityRepository<RefreshToken, AccountsDbContext>(dbContext), IRefreshTokenRepository
    {
        protected override DbSet<RefreshToken> MainTable => DbContext.RefreshTokens;

        public IDirectOperation RevokeChainBySessionId(Guid sessionId)
        {
            return new DirectUpdate<RefreshToken>(
                RefreshToken.IsActiveInSession(sessionId),
                setters => setters.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
        }

        public IDirectOperation BuildRotateOperation(Guid tokenId, Guid replacedByTokenId)
        {
            return new DirectUpdate<RefreshToken>(
                RefreshToken.IsActiveToken(tokenId),
                setters => setters
                    .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                    .SetProperty(rt => rt.ReplacedByTokenId, replacedByTokenId));
        }

        public IDirectOperation BuildCreateOperation(RefreshToken token)
        {
            return new DirectInsert<RefreshToken>(token);
        }

        public IDirectOperation BuildDeleteDeadOlderThanOperation(DateTime cutoff)
        {
            return new DirectDelete<RefreshToken>(RefreshToken.IsDeadOlderThan(cutoff));
        }
    }
}