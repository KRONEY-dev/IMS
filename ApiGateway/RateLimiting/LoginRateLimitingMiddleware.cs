using ApiGateway.Options;
using Microsoft.Extensions.Options;
using Shared.Kernel.Caching;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.RateLimiting
{
    public class LoginRateLimitingMiddleware(RequestDelegate next, IRateLimiter rateLimiter, IOptions<RateLimitingSettings> settings)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var routeMetadata = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>()?.Config.Metadata;

            if (routeMetadata is null || !routeMetadata.ContainsKey(RateLimitingRouteMetadata.RateLimitedKey))
            {
                await next(context);
                return;
            }

            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{context.Request.Path}:{clientIp}";

            var settingsValue = settings.Value;
            var allowed = await rateLimiter.TryAcquireAsync(
                key, settingsValue.Limit, TimeSpan.FromSeconds(settingsValue.WindowSeconds), context.RequestAborted);

            if (!allowed)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }

            await next(context);
        }
    }
}