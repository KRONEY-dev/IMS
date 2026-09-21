using AutoMapper;
using Shared.Kernel.Mapping;

namespace AccountsService.Application.Mappings
{
    public class AutoMapperObjectMapper : IMapperWrapper
    {
        private readonly IMapper _mapper;

        public AutoMapperObjectMapper(IMapper mapper)
        {
            _mapper = mapper;
        }

        public TDestination Map<TDestination>(object source)
        {
            return _mapper.Map<TDestination>(source);
        }
    }
}