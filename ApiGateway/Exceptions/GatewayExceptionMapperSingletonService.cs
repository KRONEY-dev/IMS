using Shared.Kernel.Exceptions;
using System.Net;

namespace ApiGateway.Exceptions
{
    public class GatewayExceptionMapperSingletonService : IExceptionMapperService
    {
        public HttpStatusCode? Map(Exception exception)
        {
            return null;
        }
    }
}
