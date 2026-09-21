using Microsoft.AspNetCore.Mvc;

namespace Shared.Kernel.AspNetCore
{
    public class BaseController<TMainService> : ControllerBase
    {
        protected readonly TMainService MainService;

        public BaseController(TMainService mainService)
        {
            MainService = mainService;
        }
    }
}