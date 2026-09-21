namespace AccountsService.Application.Services.DTOs
{
    public static class AuthServiceDTOs
    {
        public record LoginRequestDTO(string? PhoneNumber, string? Email, string Password);
        public record LoginResponseDTO(string AccessToken, RefreshTokenDTO RefreshToken);

        public record RefreshAccessTokenRequestDTO(RefreshTokenDTO RefreshToken);
        public record RefreshAccessTokenResponseDTO(string AccessToken, RefreshTokenDTO RefreshToken);

        public record LogoutRequestDTO(Guid RefreshTokenId);
        public record LogoutResponseDTO;

        public record RefreshTokenDTO(Guid Id, string Token);
    }
}