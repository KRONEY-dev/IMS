using Shared.Kernel.Exceptions;
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

            var subClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var role = principal.FindFirstValue(ClaimTypes.Role);
            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var expClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (subClaim is null || role is null || jti is null || expClaim is null
                || !Guid.TryParse(subClaim, out var userId) || !long.TryParse(expClaim, out var exp))
            {
                throw new InvalidAuthenticationContextException();
            }

            var warehouseIds = new List<Guid>();

            foreach (var claim in principal.FindAll(RequestClaimTypes.WarehouseId))
            {
                if (!Guid.TryParse(claim.Value, out var warehouseId))
                {
                    throw new InvalidAuthenticationContextException();
                }

                warehouseIds.Add(warehouseId);
            }

            var accessToken = new AccessTokenInfo(jti, DateTimeOffset.FromUnixTimeSeconds(exp));

            requestContext.Populate(userId, role, warehouseIds, accessToken);
        }
    }
}
