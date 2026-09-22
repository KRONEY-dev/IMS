using Microsoft.AspNetCore.Http;
using Shared.Kernel.Requests;

namespace Shared.Kernel.AspNetCore.Requests.Middleware
{
    public class RequestContextMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
        {
            requestContext.PopulateFromClaimsPrincipal(context.User);

            await next(context);
        }
    }
}
