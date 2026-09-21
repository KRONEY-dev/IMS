namespace Shared.Kernel.Exceptions
{
    public class ReferencedEntityInUseException(Exception innerException)
        : Exception("The entity cannot be deleted because it is still referenced by other records.", innerException);
}
