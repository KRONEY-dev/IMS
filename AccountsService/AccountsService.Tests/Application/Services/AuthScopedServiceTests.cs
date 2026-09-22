using AccountsService.Application.Options;
using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Application.Services;
using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using AccountsService.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Kernel.Caching;
using Shared.Kernel.Database;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Tests.Application.Services
{
    public class AuthScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ITokenSignerService> _tokenSignerServiceMock = new();
        private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock = new();
        private readonly Mock<IAccessTokenBlacklist> _accessTokenBlacklistMock = new();

        private static JwtSettings CreateJwtSettings(TimeSpan? gracePeriod = null)
        {
            return new JwtSettings
            {
                Issuer = "ims-accounts-tests",
                Audience = "ims-tests",
                AccessTokenLifetime = TimeSpan.FromMinutes(15),
                RefreshTokenLifetime = TimeSpan.FromDays(7),
                RefreshTokenReuseGracePeriod = gracePeriod ?? TimeSpan.FromSeconds(5),
                PrivateKeyPath = "unused-in-tests.pem"
            };
        }

        private static RefreshToken CreateRevokedToken(Guid userId, Guid sessionId, string tokenHash, DateTime revokedAt)
        {
            var token = RefreshToken.Create(userId, sessionId, tokenHash, TimeSpan.FromDays(7));

            typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedAt))!.SetValue(token, revokedAt);

            return token;
        }

        private AuthScopedService CreateSut(JwtSettings? jwtSettings = null)
        {
            return new AuthScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object, _mapperMock.Object,
                _refreshTokenRepositoryMock.Object, _userRepositoryMock.Object,
                _tokenSignerServiceMock.Object, _passwordHasherServiceMock.Object,
                _accessTokenBlacklistMock.Object, Options.Create(jwtSettings ?? CreateJwtSettings()));
        }

        private static AuthServiceDTOs.RefreshAccessTokenRequestDTO BuildRequest(Guid tokenId, string rawToken)
        {
            return new AuthServiceDTOs.RefreshAccessTokenRequestDTO(new AuthServiceDTOs.RefreshTokenDTO(tokenId, rawToken));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_TokenNotFound_ThrowsInvalidRefreshTokenException()
        {
            var tokenId = Guid.NewGuid();

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(tokenId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => sut.RefreshAccessTokenAsync(BuildRequest(tokenId, "raw-token"), CancellationToken.None));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_TokenHashMismatch_ThrowsInvalidRefreshTokenException()
        {
            var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "correct-hash", TimeSpan.FromDays(7));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _tokenSignerServiceMock
                .Setup(signer => signer.HashRefreshSecret("raw-token"))
                .Returns("different-hash");

            var sut = CreateSut();

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => sut.RefreshAccessTokenAsync(BuildRequest(token.Id, "raw-token"), CancellationToken.None));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_RevokedWithinGracePeriod_ThrowsInvalidRefreshTokenException_NotReuseDetected()
        {
            var sessionId = Guid.NewGuid();
            var token = CreateRevokedToken(Guid.NewGuid(), sessionId, "correct-hash", DateTime.UtcNow.AddSeconds(-1));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _tokenSignerServiceMock
                .Setup(signer => signer.HashRefreshSecret("raw-token"))
                .Returns("correct-hash");

            var jwtSettings = CreateJwtSettings(gracePeriod: TimeSpan.FromSeconds(5));
            var sut = CreateSut(jwtSettings);

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => sut.RefreshAccessTokenAsync(BuildRequest(token.Id, "raw-token"), CancellationToken.None));

            _refreshTokenRepositoryMock.Verify(repo => repo.RevokeChainBySessionId(It.IsAny<Guid>()), Times.Never);
            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(It.IsAny<IDirectOperation>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_RevokedPastGracePeriod_RevokesChainAndThrowsReuseDetected()
        {
            var sessionId = Guid.NewGuid();
            var token = CreateRevokedToken(Guid.NewGuid(), sessionId, "correct-hash", DateTime.UtcNow.AddMinutes(-10));
            var revokeOperation = Mock.Of<IDirectOperation>();

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _tokenSignerServiceMock
                .Setup(signer => signer.HashRefreshSecret("raw-token"))
                .Returns("correct-hash");

            _refreshTokenRepositoryMock
                .Setup(repo => repo.RevokeChainBySessionId(sessionId))
                .Returns(revokeOperation);

            var jwtSettings = CreateJwtSettings(gracePeriod: TimeSpan.FromSeconds(5));
            var sut = CreateSut(jwtSettings);

            var exception = await Assert.ThrowsAsync<RefreshTokenReuseDetectedException>(
                () => sut.RefreshAccessTokenAsync(BuildRequest(token.Id, "raw-token"), CancellationToken.None));

            Assert.Equal(sessionId, exception.SessionId);

            _refreshTokenRepositoryMock.Verify(repo => repo.RevokeChainBySessionId(sessionId), Times.Once);
            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(revokeOperation, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_NotRevokedButExpired_ThrowsRefreshTokenExpiredException()
        {
            var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "correct-hash", TimeSpan.FromSeconds(-1));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _tokenSignerServiceMock
                .Setup(signer => signer.HashRefreshSecret("raw-token"))
                .Returns("correct-hash");

            var sut = CreateSut();

            var exception = await Assert.ThrowsAsync<RefreshTokenExpiredException>(
                () => sut.RefreshAccessTokenAsync(BuildRequest(token.Id, "raw-token"), CancellationToken.None));

            Assert.Equal(token.Id, exception.TokenId);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_ValidToken_IssuesNewTokenRevokesOldAndReturnsAccessToken()
        {
            var sessionId = Guid.NewGuid();
            var user = new User("Test", "User", "test@ims.local", "+10000000000", UserRole.Worker, "password-hash");
            var token = RefreshToken.Create(user.Id, sessionId, "correct-hash", TimeSpan.FromDays(7));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _tokenSignerServiceMock
                .Setup(signer => signer.HashRefreshSecret("raw-token"))
                .Returns("correct-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _tokenSignerServiceMock
                .Setup(signer => signer.SignAccessToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<TimeSpan>()))
                .Returns("fixed-access-token");

            _tokenSignerServiceMock
                .Setup(signer => signer.GenerateRefreshSecret())
                .Returns(("raw-secret", "new-hash"));

            var sut = CreateSut();

            var response = await sut.RefreshAccessTokenAsync(BuildRequest(token.Id, "raw-token"), CancellationToken.None);

            Assert.Equal("fixed-access-token", response.AccessToken);
            Assert.True(token.IsRevoked());

            _refreshTokenRepositoryMock.Verify(
                repo => repo.Add(It.Is<RefreshToken>(newToken => newToken.SessionId == sessionId && newToken.UserId == user.Id)),
                Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_ValidToken_IncludesIssuedAtClaimOnNewAccessToken()
        {
            var sessionId = Guid.NewGuid();
            var user = new User("Test", "User", "test@ims.local", "+10000000000", UserRole.Worker, "password-hash");
            var token = RefreshToken.Create(user.Id, sessionId, "correct-hash", TimeSpan.FromDays(7));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _tokenSignerServiceMock
                .Setup(signer => signer.HashRefreshSecret("raw-token"))
                .Returns("correct-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _tokenSignerServiceMock
                .Setup(signer => signer.SignAccessToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<TimeSpan>()))
                .Returns("fixed-access-token");

            _tokenSignerServiceMock
                .Setup(signer => signer.GenerateRefreshSecret())
                .Returns(("raw-secret", "new-hash"));

            var sut = CreateSut();

            await sut.RefreshAccessTokenAsync(BuildRequest(token.Id, "raw-token"), CancellationToken.None);

            _tokenSignerServiceMock.Verify(signer => signer.SignAccessToken(
                It.Is<IEnumerable<Claim>>(claims => claims.Any(claim => claim.Type == JwtRegisteredClaimNames.Iat)),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ThrowsInvalidCredentialsException()
        {
            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("missing@ims.local", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => sut.LoginAsync(
                new AuthServiceDTOs.LoginRequestDTO(null, "missing@ims.local", "password"), CancellationToken.None));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
        {
            var user = new User("Test", "User", "test@ims.local", null, UserRole.Worker, "password-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("test@ims.local", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherServiceMock
                .Setup(hasher => hasher.Verify("password-hash", "wrong-password"))
                .Returns(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => sut.LoginAsync(
                new AuthServiceDTOs.LoginRequestDTO(null, "test@ims.local", "wrong-password"), CancellationToken.None));

            _refreshTokenRepositoryMock.Verify(repo => repo.Add(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ValidEmailCredentials_IssuesTokensAndPersistsRefreshToken()
        {
            var user = new User("Test", "User", "test@ims.local", null, UserRole.Worker, "password-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("test@ims.local", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherServiceMock
                .Setup(hasher => hasher.Verify("password-hash", "correct-password"))
                .Returns(true);

            _tokenSignerServiceMock
                .Setup(signer => signer.SignAccessToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<TimeSpan>()))
                .Returns("fixed-access-token");

            _tokenSignerServiceMock
                .Setup(signer => signer.GenerateRefreshSecret())
                .Returns(("raw-secret", "new-hash"));

            var sut = CreateSut();

            var response = await sut.LoginAsync(
                new AuthServiceDTOs.LoginRequestDTO(null, "test@ims.local", "correct-password"), CancellationToken.None);

            Assert.Equal("fixed-access-token", response.AccessToken);
            Assert.Equal("raw-secret", response.RefreshToken.Token);

            _refreshTokenRepositoryMock.Verify(
                repo => repo.Add(It.Is<RefreshToken>(token => token.UserId == user.Id)), Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_NoEmailProvided_LooksUpByPhoneNumberInstead()
        {
            var user = new User("Test", "User", null, "+10000000000", UserRole.Worker, "password-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByPhoneNumberAsync("+10000000000", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherServiceMock
                .Setup(hasher => hasher.Verify("password-hash", "correct-password"))
                .Returns(true);

            _tokenSignerServiceMock
                .Setup(signer => signer.SignAccessToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<TimeSpan>()))
                .Returns("fixed-access-token");

            _tokenSignerServiceMock
                .Setup(signer => signer.GenerateRefreshSecret())
                .Returns(("raw-secret", "new-hash"));

            var sut = CreateSut();

            await sut.LoginAsync(
                new AuthServiceDTOs.LoginRequestDTO("+10000000000", null, "correct-password"), CancellationToken.None);

            _userRepositoryMock.Verify(repo => repo.GetByPhoneNumberAsync("+10000000000", It.IsAny<CancellationToken>()), Times.Once);
            _userRepositoryMock.Verify(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_TokenNotFound_ReturnsWithoutBlacklisting()
        {
            var tokenId = Guid.NewGuid();

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(tokenId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            var sut = CreateSut();

            await sut.LogoutAsync(new AuthServiceDTOs.LogoutRequestDTO(tokenId), CancellationToken.None);

            _refreshTokenRepositoryMock.Verify(repo => repo.Remove(It.IsAny<RefreshToken>()), Times.Never);
            _accessTokenBlacklistMock.Verify(
                blacklist => blacklist.BlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_TokenBelongsToDifferentUser_ReturnsWithoutBlacklisting()
        {
            var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _requestContextMock.SetupGet(context => context.UserId).Returns(Guid.NewGuid());

            var sut = CreateSut();

            await sut.LogoutAsync(new AuthServiceDTOs.LogoutRequestDTO(token.Id), CancellationToken.None);

            _refreshTokenRepositoryMock.Verify(repo => repo.Remove(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_Valid_RemovesRefreshTokenAndBlacklistsAccessToken()
        {
            var userId = Guid.NewGuid();
            var token = RefreshToken.Create(userId, Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _requestContextMock.SetupGet(context => context.UserId).Returns(userId);
            _requestContextMock.SetupGet(context => context.AccessToken)
                .Returns(new AccessTokenInfo("jti-123", DateTimeOffset.UtcNow.AddMinutes(10)));

            var sut = CreateSut();

            await sut.LogoutAsync(new AuthServiceDTOs.LogoutRequestDTO(token.Id), CancellationToken.None);

            _refreshTokenRepositoryMock.Verify(repo => repo.Remove(token), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _accessTokenBlacklistMock.Verify(
                blacklist => blacklist.BlacklistAsync("jti-123", It.Is<TimeSpan>(ttl => ttl > TimeSpan.Zero), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_AccessTokenAlreadyExpired_RemovesTokenButDoesNotBlacklist()
        {
            var userId = Guid.NewGuid();
            var token = RefreshToken.Create(userId, Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

            _refreshTokenRepositoryMock
                .Setup(repo => repo.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _requestContextMock.SetupGet(context => context.UserId).Returns(userId);
            _requestContextMock.SetupGet(context => context.AccessToken)
                .Returns(new AccessTokenInfo("jti-123", DateTimeOffset.UtcNow.AddMinutes(-1)));

            var sut = CreateSut();

            await sut.LogoutAsync(new AuthServiceDTOs.LogoutRequestDTO(token.Id), CancellationToken.None);

            _refreshTokenRepositoryMock.Verify(repo => repo.Remove(token), Times.Once);
            _accessTokenBlacklistMock.Verify(
                blacklist => blacklist.BlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
