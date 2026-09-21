using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Kernel.AspNetCore.Requests
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.ActionArguments.Values
                .FirstOrDefault(argument => argument is not null && argument is not CancellationToken);

            if (request is not null)
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());

                if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
                {
                    var result = await validator.ValidateAsync(new ValidationContext<object>(request));

                    if (!result.IsValid)
                    {
                        context.Result = new BadRequestObjectResult(result.ToDictionary());
                        return;
                    }
                }
            }

            await next();
        }
    }
}