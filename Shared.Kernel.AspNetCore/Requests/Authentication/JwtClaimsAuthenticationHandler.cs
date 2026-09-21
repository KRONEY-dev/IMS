using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Shared.Kernel.AspNetCore.Requests.Authentication
{
    public class JwtClaimsAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorizationHeader = Request.Headers[HeaderNames.Authorization].ToString();

            if (string.IsNullOrEmpty(authorizationHeader) ||
                !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var rawToken = authorizationHeader["Bearer ".Length..].Trim();

            JwtSecurityToken jwtToken;

            try
            {
                jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
            }
            catch (ArgumentException)
            {
                return Task.FromResult(AuthenticateResult.Fail("The bearer token is not a well-formed JWT."));
            }

            var identity = new ClaimsIdentity(jwtToken.Claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}