using AccountsService.Domain.Entities.Base;
using System.Linq.Expressions;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public required Guid SessionId { get; init; }

        public required Guid UserId { get; init; }

        public string TokenHash { get; private set; }

        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public DateTime? RevokedAt { get; private set; }
        public Guid? ReplacedByTokenId { get; private set; }

        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, Guid sessionId, string tokenHash, TimeSpan lifetime)
        {
            var now = DateTime.UtcNow;

            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime)
            };
        }

        public void MarkRevoked(Guid replacedByTokenId)
        {
            if (IsRevoked())
            {
                throw new RefreshTokenAlreadyRevokedException(Id);
            }

            RevokedAt = DateTime.UtcNow;

            ReplacedByTokenId = replacedByTokenId;
        }

        public bool IsExpired()
        {
            return ExpiresAt <= DateTime.UtcNow;
        }

        public bool IsRevoked()
        {
            return RevokedAt.HasValue && RevokedAt.Value != default;
        }

        public static Expression<Func<RefreshToken, bool>> IsActiveInSession(Guid sessionId)
        {
            return rt => rt.SessionId == sessionId && rt.RevokedAt == null;
        }

        public static Expression<Func<RefreshToken, bool>> IsDeadOlderThan(DateTime cutoff)
        {
            return rt => (rt.RevokedAt != null && rt.RevokedAt < cutoff) || (rt.RevokedAt == null && rt.ExpiresAt < cutoff);
        }
    }
}