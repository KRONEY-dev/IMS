using ApiGateway.Authorization;
using Scalar.AspNetCore;

namespace ApiGateway.Endpoints
{
    public static class ScalarDocsEndpoints
    {
        public static void MapScalarDocumentation(this WebApplication app)
        {
            app.MapOpenApi().RequireAuthorization(DocsAccessPolicies.Allowlist);

            app.MapScalarApiReference("scalar", options =>
            {
                options.AddDocument("accounts", "Accounts API", "/accounts/openapi/v1.json");
                options.AddDocument("inventory", "Inventory API", "/inventory/openapi/v1.json");
                options.AddDocument("gateway", "Gateway Admin API", "/openapi/v1.json");
            }).RequireAuthorization(DocsAccessPolicies.Allowlist);
        }
    }
}