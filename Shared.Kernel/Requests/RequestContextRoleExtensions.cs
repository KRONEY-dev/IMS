using Shared.Kernel.Exceptions;

namespace Shared.Kernel.Requests
{
    public static class RequestContextRoleExtensions
    {
        public static TRole GetRole<TRole>(this IRequestContext context)
            where TRole : struct, Enum
        {
            return Enum.Parse<TRole>(context.Role);
        }

        public static void EnsureMinimumRole<TRole>(this IRequestContext context, TRole minimumRole)
            where TRole : struct, Enum
        {
            if (Convert.ToInt32(context.GetRole<TRole>()) > Convert.ToInt32(minimumRole))
            {
                throw new InsufficientPermissionsException();
            }
        }

        public static void EnsureWarehouseAccess<TRole>(this IRequestContext context, Guid warehouseId, TRole bypassAtOrAbove)
            where TRole : struct, Enum
        {
            if (Convert.ToInt32(context.GetRole<TRole>()) <= Convert.ToInt32(bypassAtOrAbove))
            {
                return;
            }

            if (!context.WarehouseIds.Contains(warehouseId))
            {
                throw new InsufficientPermissionsException();
            }
        }

        public static void EnsureMinimumRoleOrSelf<TRole>(this IRequestContext context, Guid targetUserId, TRole minimumRole)
            where TRole : struct, Enum
        {
            if (context.UserId == targetUserId)
            {
                return;
            }

            context.EnsureMinimumRole(minimumRole);
        }
    }
}