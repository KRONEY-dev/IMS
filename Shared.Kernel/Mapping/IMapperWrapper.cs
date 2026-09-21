namespace Shared.Kernel.Mapping
{
    public interface IMapperWrapper
    {
        TDestination Map<TDestination>(object source);
    }
}