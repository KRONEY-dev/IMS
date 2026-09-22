using ApiGateway.Authorization;
using StackExchange.Redis;
using System.Net;

namespace ApiGateway.Endpoints
{
    public record AddNetworkRequestDTO(string Network, string Description);
    public record RemoveNetworkRequestDTO(string Network);
    public record NetworkEntryDTO(string Network, string Description);
    public record ListNetworksResponseDTO(IReadOnlyList<NetworkEntryDTO> Networks);

    public static class DocsAccessEndpoints
    {
        private const int MaxDescriptionLength = 30;

        public static void MapDocsAccessEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/gateway/docs-access").RequireAuthorization(DocsAccessPolicies.Admin);

            group.MapGet("/networks", async (IConnectionMultiplexer connectionMultiplexer) =>
            {
                var database = connectionMultiplexer.GetDatabase();
                var entries = await database.HashGetAllAsync(IpAllowlistAuthorizationHandler.RedisKey);

                return Results.Ok(new ListNetworksResponseDTO(
                    [.. entries
                        .Select(entry => new NetworkEntryDTO(entry.Name.ToString(), entry.Value.ToString()))
                        .OrderBy(entry => entry.Network)]));
            })
                .WithName("ListAllowedNetworks")
                .WithSummary("List of CIDR networks allowed to access the documentation, with descriptions.");

            group.MapPost("/networks", async (AddNetworkRequestDTO request, IConnectionMultiplexer connectionMultiplexer) =>
            {
                if (!IPNetwork.TryParse(request.Network, out _))
                {
                    return Results.BadRequest($"'{request.Network}' is not a valid CIDR network.");
                }

                if (request.Description.Length > MaxDescriptionLength)
                {
                    return Results.BadRequest($"Description must be at most {MaxDescriptionLength} characters.");
                }

                var database = connectionMultiplexer.GetDatabase();
                await database.HashSetAsync(IpAllowlistAuthorizationHandler.RedisKey, request.Network, request.Description);

                return Results.Ok();
            })
                .WithName("AddAllowedNetwork")
                .WithSummary("Adds a CIDR network (e.g. \"192.168.1.0/24\") with a description (up to 30 characters) to the allowlist.");

            group.MapPost("/networks/remove", async (RemoveNetworkRequestDTO request, IConnectionMultiplexer connectionMultiplexer) =>
            {
                var database = connectionMultiplexer.GetDatabase();
                await database.HashDeleteAsync(IpAllowlistAuthorizationHandler.RedisKey, request.Network);

                return Results.Ok();
            })
                .WithName("RemoveAllowedNetwork")
                .WithSummary("Removes a CIDR network from the allowlist.");
        }
    }
}
