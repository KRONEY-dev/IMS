using AccountsService.Application.Options;
using AccountsService.Application.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AccountsService.Infrastructure.Services
{
    public class TokenSignerSingletonService : ITokenSignerService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly SigningCredentials _signingCredentials;

        public TokenSignerSingletonService(IOptions<JwtSettings> options)
        {
            _jwtSettings = options.Value;

            var privateRsa = RSA.Create();
            privateRsa.ImportFromPem(File.ReadAllText(_jwtSettings.PrivateKeyPath));
            _signingCredentials = new SigningCredentials(new RsaSecurityKey(privateRsa), SecurityAlgorithms.RsaSha256);
        }

        public string SignAccessToken(IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(lifetime),
                signingCredentials: _signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string RawSecret, string TokenHash) GenerateRefreshSecret()
        {
            var rawSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashRefreshSecret(rawSecret);

            return (rawSecret, tokenHash);
        }

        public string HashRefreshSecret(string rawSecret)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret)));
        }
    }
}