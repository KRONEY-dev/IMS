using InventoryService.Domain.Enums;
using Shared.Kernel.Requests;

namespace InventoryService.Application.Common
{
    public static class RequestContextExtensions
    {
        public static UserRole GetUserRole(this IRequestContext context)
        {
            return context.GetRole<UserRole>();
        }
    }
}