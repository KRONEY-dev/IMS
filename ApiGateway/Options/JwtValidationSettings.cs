namespace ApiGateway.Options
{
    public class JwtValidationSettings
    {
        public string Issuer { get; init; } = default!;
        public string Audience { get; init; } = default!;
        public string PublicKeyPath { get; init; } = default!;
    }
}