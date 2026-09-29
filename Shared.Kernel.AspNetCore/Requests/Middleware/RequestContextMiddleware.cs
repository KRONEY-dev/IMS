using Microsoft.AspNetCore.Http;
using Shared.Kernel.Requests;

namespace Shared.Kernel.AspNetCore.Requests.Middleware
{
    public class RequestContextMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
        {
            requestContext.PopulateFromClaimsPrincipal(context.User);

            await _next(context);
        }
    }
}