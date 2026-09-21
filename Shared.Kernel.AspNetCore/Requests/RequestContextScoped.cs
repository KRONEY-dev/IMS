using Shared.Kernel.Requests;

namespace Shared.Kernel.AspNetCore.Requests
{
    public class RequestContextScoped : IRequestContext
    {
        private Guid? _userId;
        private string? _role;
        private IReadOnlyList<Guid>? _warehouseIds;
        private AccessTokenInfo? _accessToken;

        public Guid UserId => _userId ?? throw new InvalidOperationException("RequestContext is not populated for this request.");
        public string Role => _role ?? throw new InvalidOperationException("RequestContext is not populated for this request.");
        public IReadOnlyList<Guid> WarehouseIds => _warehouseIds ?? throw new InvalidOperationException("RequestContext is not populated for this request.");
        public AccessTokenInfo AccessToken => _accessToken ?? throw new InvalidOperationException("RequestContext is not populated for this request.");

        public void Populate(Guid userId, string role, IReadOnlyList<Guid> warehouseIds, AccessTokenInfo accessToken)
        {
            _userId = userId;
            _role = role;
            _warehouseIds = warehouseIds;
            _accessToken = accessToken;
        }
    }
}