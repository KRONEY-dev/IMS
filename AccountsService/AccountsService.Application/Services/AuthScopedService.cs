using AccountsService.Application.Options;
using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using AccountsService.Domain.Entities;
using Microsoft.Extensions.Options;
using Shared.Kernel.Caching;
using Shared.Kernel.Database;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Application.Services
{
    public class AuthScopedService : BaseService, IAuthService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;

        private readonly ITokenSignerService _tokenSignerService;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IAccessTokenBlacklist _accessTokenBlacklist;

        private readonly JwtSettings _jwtSettings;

        private static string? _dummyPasswordHash;

        public AuthScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext, IMapperWrapper mapper,
            IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository,
            ITokenSignerService tokenSignerService, IPasswordHasherService passwordHasherService,
            IAccessTokenBlacklist accessTokenBlacklist, IOptions<JwtSettings> jwtSettings)
            : base(unitOfWork, requestContext, mapper)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;

            _tokenSignerService = tokenSignerService;
            _passwordHasherService = passwordHasherService;
            _accessTokenBlacklist = accessTokenBlacklist;

            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthServiceDTOs.LoginResponseDTO> LoginAsync(AuthServiceDTOs.LoginRequestDTO request,
            CancellationToken cancellationToken)
        {
            User? user = null;

            if (!string.IsNullOrEmpty(request.Email))
            {
                user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
            }

            if (user is null)
            {
                _dummyPasswordHash ??= _passwordHasherService.Hash(Guid.NewGuid().ToString());
                _passwordHasherService.Verify(_dummyPasswordHash, request.Password);

                throw new InvalidCredentialsException();
            }

            if (!_passwordHasherService.Verify(user.PasswordHash, request.Password))
            {
                throw new InvalidCredentialsException();
            }

            var sessionId = Guid.NewGuid();

            var tokenData = GenerateTokens(user, sessionId);

            _refreshTokenRepository.Add(tokenData.RefreshToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthServiceDTOs.LoginResponseDTO(tokenData.AccessToken,
                new AuthServiceDTOs.RefreshTokenDTO(tokenData.RefreshToken.Id, tokenData.RawSecret));
        }

        public async Task<AuthServiceDTOs.RefreshAccessTokenResponseDTO> RefreshAccessTokenAsync(
            AuthServiceDTOs.RefreshAccessTokenRequestDTO request, CancellationToken cancellationToken)
        {
            var refreshTokenFromRequest = request.RefreshToken;

            var token = await _refreshTokenRepository.GetByIdAsync(refreshTokenFromRequest.Id, cancellationToken);

            if (token is null || token.TokenHash != _tokenSignerService.HashRefreshSecret(refreshTokenFromRequest.Token))
            {
                throw new InvalidRefreshTokenException();
            }

            if (token.IsRevoked())
            {
                throw await BuildReuseExceptionAsync(token, cancellationToken);
            }

            if (token.IsExpired())
            {
                throw new RefreshTokenExpiredException(token.Id);
            }

            var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken)
                ?? throw new InvalidCredentialsException();

            var tokenData = GenerateTokens(user, token.SessionId);
            var newRefreshToken = tokenData.RefreshToken;

            var rotateOperations = new List<IDirectOperation>
            {
                _refreshTokenRepository.BuildCreateOperation(newRefreshToken),
                _refreshTokenRepository.BuildRotateOperation(token.Id, newRefreshToken.Id)
            };

            var rotated = await UnitOfWork.ExecuteInTransactionAsync(rotateOperations, cancellationToken);

            if (!rotated)
            {
                // Someone else rotated this exact token between our read and our write attempt. Our
                // tracked copy is stale (EF's identity map won't re-query it), so reload it in place
                // to see the real RevokedAt - that way a genuinely concurrent race (two requests
                // racing the same still-valid token) is judged by the same grace period as a
                // sequential reuse, instead of unconditionally revoking the winner's brand-new
                // session for a race it did nothing wrong in.
                await _refreshTokenRepository.ReloadAsync(token, cancellationToken);

                throw await BuildReuseExceptionAsync(token, cancellationToken);
            }

            return new AuthServiceDTOs.RefreshAccessTokenResponseDTO(tokenData.AccessToken,
                new AuthServiceDTOs.RefreshTokenDTO(newRefreshToken.Id, tokenData.RawSecret));
        }

        private async Task<Exception> BuildReuseExceptionAsync(RefreshToken revokedToken, CancellationToken cancellationToken)
        {
            var withinGracePeriod = revokedToken.RevokedAt is not null
                && DateTime.UtcNow - revokedToken.RevokedAt.Value <= _jwtSettings.RefreshTokenReuseGracePeriod;

            if (withinGracePeriod)
            {
                return new InvalidRefreshTokenException();
            }

            var tokensRevokeOperation = _refreshTokenRepository.RevokeChainBySessionId(revokedToken.SessionId);
            await UnitOfWork.ExecuteInTransactionAsync(tokensRevokeOperation, cancellationToken);

            return new RefreshTokenReuseDetectedException(revokedToken.SessionId);
        }

        public async Task<AuthServiceDTOs.LogoutResponseDTO> LogoutAsync(AuthServiceDTOs.LogoutRequestDTO request,
            CancellationToken cancellationToken)
        {
            var token = await _refreshTokenRepository.GetByIdAsync(request.RefreshTokenId, cancellationToken);

            if (token is null || token.UserId != RequestContext.UserId)
            {
                return new AuthServiceDTOs.LogoutResponseDTO();
            }

            _refreshTokenRepository.Remove(token);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            var ttl = RequestContext.AccessToken.ExpiresAt - DateTimeOffset.UtcNow;

            if (ttl > TimeSpan.Zero)
            {
                await _accessTokenBlacklist.BlacklistAsync(RequestContext.AccessToken.Jti, ttl, cancellationToken);
            }

            return new AuthServiceDTOs.LogoutResponseDTO();
        }

        private readonly record struct GenerateTokensResult(string AccessToken, RefreshToken RefreshToken, string RawSecret);
        private GenerateTokensResult GenerateTokens(User user, Guid sessionId)
        {
            var claims = BuildClaims(user);
            var accessToken = _tokenSignerService.SignAccessToken(claims, _jwtSettings.AccessTokenLifetime);

            var (rawSecret, tokenHash) = _tokenSignerService.GenerateRefreshSecret();
            var refreshToken = RefreshToken.Create(user.Id, sessionId, tokenHash, _jwtSettings.RefreshTokenLifetime);

            return new GenerateTokensResult(accessToken, refreshToken, rawSecret);
        }

        private static List<Claim> BuildClaims(User user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            claims.AddRange(user.WarehouseIds.Select(id => new Claim(RequestClaimTypes.WarehouseId, id.ToString())));

            return claims;
        }
    }
}