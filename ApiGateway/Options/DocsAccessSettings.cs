namespace ApiGateway.Options
{
    public class DocsAccessSettings
    {
        public List<string> AllowedNetworks { get; init; } = [];
        public bool RequireIpAllowlist { get; init; } = true;
        public int MaxDescriptionLength { get; init; } = 30;
    }
}