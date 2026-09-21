using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Authorization
{
    public static class DocsAccessAuthorizationExtensions
    {
        public static IServiceCollection AddDocsAccessAuthorization(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IAuthorizationHandler, IpAllowlistAuthorizationHandler>();

            services.AddAuthorizationBuilder()
                .AddPolicy(DocsAccessPolicies.Allowlist, policy => policy.Requirements.Add(new IpAllowlistRequirement()))
                .AddPolicy(DocsAccessPolicies.Admin, policy =>
                {
                    policy.Requirements.Add(new IpAllowlistRequirement());
                    policy.RequireRole("Admin");
                });

            return services;
        }
    }
}