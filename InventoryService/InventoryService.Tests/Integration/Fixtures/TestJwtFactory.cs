using InventoryService.Domain.Enums;
using Shared.Kernel.Requests;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InventoryService.Tests.Integration.Fixtures
{
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