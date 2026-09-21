namespace Shared.Kernel.Exceptions
{
    public class UniqueConstraintViolationException(Exception innerException)
        : Exception("A unique constraint was violated while saving changes.", innerException);
}