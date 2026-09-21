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
                .WithSummary("Список CIDR-мереж, яким дозволено доступ до документації, з описом.");

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
                .WithSummary("Додає CIDR-мережу (напр. \"192.168.1.0/24\") з описом (до 30 символів) до allowlist.");

            group.MapPost("/networks/remove", async (RemoveNetworkRequestDTO request, IConnectionMultiplexer connectionMultiplexer) =>
            {
                var database = connectionMultiplexer.GetDatabase();
                await database.HashDeleteAsync(IpAllowlistAuthorizationHandler.RedisKey, request.Network);

                return Results.Ok();
            })
                .WithName("RemoveAllowedNetwork")
                .WithSummary("Видаляє CIDR-мережу з allowlist.");
        }
    }
}
