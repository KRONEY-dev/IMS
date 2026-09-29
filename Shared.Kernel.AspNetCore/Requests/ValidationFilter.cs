using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Shared.Kernel.AspNetCore.Requests
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var bodyParameterName = context.ActionDescriptor.Parameters
                .OfType<ControllerParameterDescriptor>()
                .FirstOrDefault(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)?.Name;

            var request = bodyParameterName is not null && context.ActionArguments.TryGetValue(bodyParameterName, out var value)
                ? value
                : null;

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