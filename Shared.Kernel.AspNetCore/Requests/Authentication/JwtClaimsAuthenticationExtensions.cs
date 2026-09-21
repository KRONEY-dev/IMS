using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Kernel.AspNetCore.Requests.Authentication
{
    public static class JwtClaimsAuthenticationExtensions
    {
        public const string SchemeName = "JwtClaims";

        public static AuthenticationBuilder AddJwtClaimsAuthentication(this IServiceCollection services)
        {
            return services.AddAuthentication(SchemeName)
                .AddScheme<AuthenticationSchemeOptions, JwtClaimsAuthenticationHandler>(SchemeName, configureOptions: null);
        }
    }
}