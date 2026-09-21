using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Exceptions;
using System.Net;

namespace Shared.Kernel.AspNetCore.Requests.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private RequestDelegate _next;
        private IExceptionMapperService _statusMapper;
        private ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, IExceptionMapperService statusMapper,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _statusMapper = statusMapper;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var statusCode = ex switch
                {
                    InsufficientPermissionsException => HttpStatusCode.Forbidden,
                    NotFoundException => HttpStatusCode.NotFound,
                    _ => _statusMapper.Map(ex) ?? HttpStatusCode.InternalServerError
                };

                if (statusCode == HttpStatusCode.InternalServerError)
                {
                    _logger.LogError(ex, "Unhandled exception");
                }

                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = statusCode == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : ex.Message
                });
            }
        }
    }
}