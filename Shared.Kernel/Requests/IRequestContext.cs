namespace Shared.Kernel.Requests
{
    public readonly record struct AccessTokenInfo(string Jti, DateTimeOffset ExpiresAt);

    public interface IRequestContext
    {
        Guid UserId { get; }
        string Role { get; }
        IReadOnlyList<Guid> WarehouseIds { get; }
        AccessTokenInfo AccessToken { get; }

        void Populate(Guid userId, string role, IReadOnlyList<Guid> warehouseIds, AccessTokenInfo accessToken);
    }
}