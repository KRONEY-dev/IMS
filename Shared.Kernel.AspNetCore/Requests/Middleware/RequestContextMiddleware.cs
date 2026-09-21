using Microsoft.AspNetCore.Http;
using Shared.Kernel.Requests;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Shared.Kernel.AspNetCore.Requests.Middleware
{
    public class RequestContextMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = Guid.Parse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
                var role = context.User.FindFirstValue(ClaimTypes.Role)!;

                var accessToken = new AccessTokenInfo(
                    context.User.FindFirstValue(JwtRegisteredClaimNames.Jti)!,
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(context.User.FindFirstValue(JwtRegisteredClaimNames.Exp)!)));

                var warehouseIds = context.User.FindAll(RequestClaimTypes.WarehouseId)
                    .Select(claim => Guid.Parse(claim.Value))
                    .ToList();

                requestContext.Populate(userId, role, warehouseIds, accessToken);
            }

            await next(context);
        }
    }
}