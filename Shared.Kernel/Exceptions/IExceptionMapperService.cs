using System.Net;

namespace Shared.Kernel.Exceptions
{
    public interface IExceptionMapperService
    {
        HttpStatusCode? Map(Exception exception);
    }
}