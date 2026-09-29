using ApiGateway.Options;
using Microsoft.Extensions.Options;
using Shared.Kernel.Caching;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.RateLimiting
{
    public class LoginRateLimitingMiddleware(RequestDelegate next, IRateLimiter rateLimiter,
        IOptions<RateLimitingSettings> settings, ILogger<LoginRateLimitingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var routeConfig = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>()?.Config;

            if (routeConfig?.Metadata is null || !routeConfig.Metadata.ContainsKey(RateLimitingRouteMetadata.RateLimitedKey))
            {
                await next(context);
                return;
            }

            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{routeConfig.RouteId}:{clientIp}";

            var settingsValue = settings.Value;
            var allowed = await rateLimiter.TryAcquireAsync(
                key, settingsValue.Limit, TimeSpan.FromSeconds(settingsValue.WindowSeconds), context.RequestAborted);

            if (!allowed)
            {
                logger.LogWarning("Rate limit exceeded for {RouteId} from {ClientIp}", routeConfig.RouteId, clientIp);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }

            await next(context);
        }
    }
}