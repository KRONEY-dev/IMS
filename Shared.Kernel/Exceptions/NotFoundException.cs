namespace Shared.Kernel.Exceptions
{
    public class NotFoundException(string entityName, object id)
        : Exception($"{entityName} '{id}' was not found.")
    {
        public string EntityName { get; } = entityName;
        public object Id { get; } = id;
    }
}