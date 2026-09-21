namespace Shared.Kernel.Exceptions
{
    public class InsufficientPermissionsException() : Exception("Caller does not have permission to perform this action.");
}