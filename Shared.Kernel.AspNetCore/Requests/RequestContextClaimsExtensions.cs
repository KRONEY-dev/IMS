using Shared.Kernel.Requests;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Shared.Kernel.AspNetCore.Requests
{
    public static class RequestContextClaimsExtensions
    {
        public static void PopulateFromClaimsPrincipal(this IRequestContext requestContext, ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var role = principal.FindFirstValue(ClaimTypes.Role)!;

            var accessToken = new AccessTokenInfo(
                principal.FindFirstValue(JwtRegisteredClaimNames.Jti)!,
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Exp)!)));

            var warehouseIds = principal.FindAll(RequestClaimTypes.WarehouseId)
                .Select(claim => Guid.Parse(claim.Value))
                .ToList();

            requestContext.Populate(userId, role, warehouseIds, accessToken);
        }
    }
}
