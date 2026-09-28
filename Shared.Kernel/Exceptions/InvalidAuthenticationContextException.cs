namespace Shared.Kernel.Exceptions
{
    public class InvalidAuthenticationContextException() : Exception("The authenticated request is missing required claims.");
}
