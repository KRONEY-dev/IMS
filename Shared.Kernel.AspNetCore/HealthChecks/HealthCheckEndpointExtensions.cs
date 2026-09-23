using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Shared.Kernel.AspNetCore.HealthChecks
{
    public static class HealthCheckEndpointExtensions
    {
        public static void MapLivenessAndReadinessHealthChecks(this IEndpointRouteBuilder app)
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
        }
    }
}
