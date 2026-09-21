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

            if (user is null || !_passwordHasherService.Verify(user.PasswordHash, request.Password))
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
                var withinGracePeriod = DateTime.UtcNow - token.RevokedAt!.Value <= _jwtSettings.RefreshTokenReuseGracePeriod;

                if (withinGracePeriod)
                {
                    throw new InvalidRefreshTokenException();
                }

                var tokensRevokeOperation = _refreshTokenRepository.RevokeChainBySessionId(token.SessionId);
                await UnitOfWork.ExecuteInTransactionAsync(tokensRevokeOperation, cancellationToken);

                throw new RefreshTokenReuseDetectedException(token.SessionId);
            }

            if (token.IsExpired())
            {
                throw new RefreshTokenExpiredException(token.Id);
            }

            var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken)
                ?? throw new InvalidCredentialsException();

            var tokenData = GenerateTokens(user, token.SessionId);
            var newRefreshToken = tokenData.RefreshToken;

            token.MarkRevoked(newRefreshToken.Id);
            _refreshTokenRepository.Add(newRefreshToken);

            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthServiceDTOs.RefreshAccessTokenResponseDTO(tokenData.AccessToken,
                new AuthServiceDTOs.RefreshTokenDTO(newRefreshToken.Id, tokenData.RawSecret));
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
                new(ClaimTypes.Role, user.Role.ToString())
            };

            claims.AddRange(user.WarehouseIds.Select(id => new Claim(RequestClaimTypes.WarehouseId, id.ToString())));

            return claims;
        }
    }
}