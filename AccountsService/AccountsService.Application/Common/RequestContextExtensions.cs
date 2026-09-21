using AccountsService.Domain.Entities;
using Shared.Kernel.Requests;

namespace AccountsService.Application.Common
{
    public static class RequestContextExtensions
    {
        public static UserRole GetUserRole(this IRequestContext context)
        {
            return context.GetRole<UserRole>();
        }
    }
}