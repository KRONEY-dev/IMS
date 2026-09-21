using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shared.Kernel.AspNetCore.Requests.Authentication;

namespace Shared.Kernel.AspNetCore.OpenApi
{
    public static class OpenApiExtensions
    {
        public static IServiceCollection AddOpenApiWithBearerAuth(this IServiceCollection services,
            string? gatewayBasePath = null, string schemeName = JwtClaimsAuthenticationExtensions.SchemeName)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    var transformer = new BearerSecuritySchemeTransformer(
                        context.ApplicationServices.GetRequiredService<IAuthenticationSchemeProvider>(), schemeName);

                    return transformer.TransformAsync(document, context, cancellationToken);
                });

                if (gatewayBasePath is not null)
                {
                    options.AddDocumentTransformer((document, _, _) =>
                    {
                        document.Servers = [new OpenApiServer { Url = gatewayBasePath }];

                        return Task.CompletedTask;
                    });
                }
            });

            return services;
        }
    }
}