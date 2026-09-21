namespace ApiGateway.Options
{
    public class RateLimitingSettings
    {
        public int Limit { get; init; } = 5;
        public int WindowSeconds { get; init; } = 60;
    }
}
