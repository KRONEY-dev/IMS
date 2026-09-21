using Shared.Kernel.Database;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;

namespace Shared.Kernel.Services
{
    public abstract class BaseService
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly IRequestContext RequestContext;
        protected readonly IMapperWrapper Mapper;

        protected BaseService(IUnitOfWork unitOfWork, IRequestContext requestContext, IMapperWrapper mapper)
        {
            UnitOfWork = unitOfWork;
            RequestContext = requestContext;
            Mapper = mapper;
        }
    }
}