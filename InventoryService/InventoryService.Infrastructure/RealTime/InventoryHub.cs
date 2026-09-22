using InventoryService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.Kernel.AspNetCore.Requests;
using Shared.Kernel.Requests;

namespace InventoryService.Infrastructure.RealTime
{
    [Authorize]
    public class InventoryHub : Hub
    {
        private readonly IRequestContext _requestContext;
        private readonly IUserConnectionRegistry _userConnectionRegistry;

        public InventoryHub(IRequestContext requestContext, IUserConnectionRegistry userConnectionRegistry)
        {
            _requestContext = requestContext;
            _userConnectionRegistry = userConnectionRegistry;
        }

        public override async Task OnConnectedAsync()
        {
            _requestContext.PopulateFromClaimsPrincipal(Context.User!);
            await _userConnectionRegistry.AddConnectionAsync(_requestContext.UserId, Context.ConnectionId, Context.ConnectionAborted);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _requestContext.PopulateFromClaimsPrincipal(Context.User!);
            await _userConnectionRegistry.RemoveConnectionAsync(_requestContext.UserId, Context.ConnectionId, CancellationToken.None);

            await base.OnDisconnectedAsync(exception);
        }

        public Task JoinWarehouse(Guid warehouseId)
        {
            _requestContext.PopulateFromClaimsPrincipal(Context.User!);
            _requestContext.EnsureWarehouseAccess(warehouseId, UserRole.Admin);

            return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(warehouseId));
        }

        public static string GroupName(Guid warehouseId)
        {
            return $"warehouse:{warehouseId}";
        }
    }
}
