namespace AccountsService.Application.Options
{
    public class JwtSettings
    {
        public string Issuer { get; init; } = default!;
        public string Audience { get; init; } = default!;
        public TimeSpan AccessTokenLifetime { get; init; }
        public TimeSpan RefreshTokenLifetime { get; init; }
        public TimeSpan RefreshTokenReuseGracePeriod { get; init; }
        public string PrivateKeyPath { get; init; } = default!;
    }
}