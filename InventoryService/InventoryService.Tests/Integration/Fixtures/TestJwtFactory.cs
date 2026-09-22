using InventoryService.Domain.Enums;
using Shared.Kernel.Requests;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InventoryService.Tests.Integration.Fixtures
{
    // Builds an unsigned JWT-shaped token for tests. This is safe here specifically because
    // InventoryService never verifies a JWT signature itself (Shared.Kernel.AspNetCore's
    // JwtClaimsAuthenticationHandler only decodes claims) - the Gateway is the only place
    // that checks the signature, and requests reach InventoryService.API directly in tests.
    public static class TestJwtFactory
    {
        public static string CreateToken(Guid userId, UserRole role, IEnumerable<Guid>? warehouseIds = null)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.Role, role.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (warehouseIds is not null)
            {
                claims.AddRange(warehouseIds.Select(id => new Claim(RequestClaimTypes.WarehouseId, id.ToString())));
            }

            var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(1));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
