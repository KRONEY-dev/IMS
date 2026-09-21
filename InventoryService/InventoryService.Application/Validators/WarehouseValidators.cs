using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class CreateWarehouseRequestDTOValidator : AbstractValidator<WarehouseServiceDTOs.CreateWarehouseRequestDTO>
    {
        public CreateWarehouseRequestDTOValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(request => request.Location)
                .NotEmpty();

            RuleFor(request => request.WorkingHours)
                .NotNull();
        }
    }

    public class GetWarehouseByIdRequestDTOValidator : AbstractValidator<WarehouseServiceDTOs.GetWarehouseByIdRequestDTO>
    {
        public GetWarehouseByIdRequestDTOValidator()
        {
            RuleFor(request => request.WarehouseId)
                .NotEmpty();
        }
    }

    public class UpdateWarehouseStatusRequestDTOValidator : AbstractValidator<WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO>
    {
        public UpdateWarehouseStatusRequestDTOValidator()
        {
            RuleFor(request => request.WarehouseId)
                .NotEmpty();

            RuleFor(request => request.Status)
                .IsInEnum();
        }
    }

    public class UpdateWarehouseWorkingHoursRequestDTOValidator : AbstractValidator<WarehouseServiceDTOs.UpdateWarehouseWorkingHoursRequestDTO>
    {
        public UpdateWarehouseWorkingHoursRequestDTOValidator()
        {
            RuleFor(request => request.WarehouseId)
                .NotEmpty();

            RuleFor(request => request.WorkingHours)
                .NotNull();
        }
    }

    public class DeleteWarehouseRequestDTOValidator : AbstractValidator<WarehouseServiceDTOs.DeleteWarehouseRequestDTO>
    {
        public DeleteWarehouseRequestDTOValidator()
        {
            RuleFor(request => request.WarehouseId)
                .NotEmpty();
        }
    }
}