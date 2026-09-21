namespace Shared.Kernel.Exceptions
{
    public class ConcurrencyConflictException(Exception innerException)
        : Exception("A concurrency conflict occurred while saving changes.", innerException);
}