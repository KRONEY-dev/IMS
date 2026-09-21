using Shared.Kernel.Exceptions;
using System.Net;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Application.Exceptions
{
    public class AccountsExceptionMapperSingletonService : IExceptionMapperService
    {
        public HttpStatusCode? Map(Exception exception) => exception switch
        {
            InvalidCredentialsException => HttpStatusCode.Unauthorized,
            InvalidRefreshTokenException => HttpStatusCode.Unauthorized,
            RefreshTokenExpiredException => HttpStatusCode.Unauthorized,
            RefreshTokenReuseDetectedException => HttpStatusCode.Unauthorized,
            EmailAlreadyTakenException => HttpStatusCode.Conflict,
            RefreshTokenAlreadyRevokedException => HttpStatusCode.Conflict,
            PhoneNumberAlreadyTakenException => HttpStatusCode.Conflict,
            _ => null
        };
    }
}