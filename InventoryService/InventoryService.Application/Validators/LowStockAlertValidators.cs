using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class GetLowStockAlertByIdRequestDTOValidator : AbstractValidator<LowStockAlertServiceDTOs.GetLowStockAlertByIdRequestDTO>
    {
        public GetLowStockAlertByIdRequestDTOValidator()
        {
            RuleFor(request => request.LowStockAlertId)
                .NotEmpty();
        }
    }

    public class ResolveLowStockAlertRequestDTOValidator : AbstractValidator<LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO>
    {
        public ResolveLowStockAlertRequestDTOValidator()
        {
            RuleFor(request => request.LowStockAlertId)
                .NotEmpty();
        }
    }
}