using System.Security.Claims;

namespace AccountsService.Application.Services.Interfaces
{
    public interface ITokenSignerService
    {
        string SignAccessToken(IEnumerable<Claim> claims, TimeSpan lifetime);
        (string RawSecret, string TokenHash) GenerateRefreshSecret();
        string HashRefreshSecret(string rawSecret);
    }
}