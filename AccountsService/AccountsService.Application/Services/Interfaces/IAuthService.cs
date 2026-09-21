using AccountsService.Application.Services.DTOs;

namespace AccountsService.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthServiceDTOs.LoginResponseDTO> LoginAsync(AuthServiceDTOs.LoginRequestDTO request, CancellationToken cancellationToken);
        Task<AuthServiceDTOs.RefreshAccessTokenResponseDTO> RefreshAccessTokenAsync(AuthServiceDTOs.RefreshAccessTokenRequestDTO request, CancellationToken cancellationToken);
        Task<AuthServiceDTOs.LogoutResponseDTO> LogoutAsync(AuthServiceDTOs.LogoutRequestDTO request, CancellationToken cancellationToken);
    }
}