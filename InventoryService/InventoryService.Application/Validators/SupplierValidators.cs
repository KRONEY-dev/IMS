using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class CreateSupplierRequestDTOValidator : AbstractValidator<SupplierServiceDTOs.CreateSupplierRequestDTO>
    {
        public CreateSupplierRequestDTOValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty();
        }
    }

    public class GetSupplierByIdRequestDTOValidator : AbstractValidator<SupplierServiceDTOs.GetSupplierByIdRequestDTO>
    {
        public GetSupplierByIdRequestDTOValidator()
        {
            RuleFor(request => request.SupplierId)
                .NotEmpty();
        }
    }

    public class UpdateSupplierRequestDTOValidator : AbstractValidator<SupplierServiceDTOs.UpdateSupplierRequestDTO>
    {
        public UpdateSupplierRequestDTOValidator()
        {
            RuleFor(request => request.SupplierId)
                .NotEmpty();

            RuleFor(request => request.Name)
                .NotEmpty();
        }
    }

    public class DeleteSupplierRequestDTOValidator : AbstractValidator<SupplierServiceDTOs.DeleteSupplierRequestDTO>
    {
        public DeleteSupplierRequestDTOValidator()
        {
            RuleFor(request => request.SupplierId)
                .NotEmpty();
        }
    }
}