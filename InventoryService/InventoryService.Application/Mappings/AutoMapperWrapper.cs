using AutoMapper;
using Shared.Kernel.Mapping;

namespace InventoryService.Application.Mappings
{
    public class AutoMapperWrapper : IMapperWrapper
    {
        private readonly IMapper _mapper;

        public AutoMapperWrapper(IMapper mapper)
        {
            _mapper = mapper;
        }

        public TDestination Map<TDestination>(object source)
        {
            return _mapper.Map<TDestination>(source);
        }
    }
}