using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class CreateSupplierOrderItemDTOValidator : AbstractValidator<SupplierOrderServiceDTOs.CreateSupplierOrderItemDTO>
    {
        public CreateSupplierOrderItemDTOValidator()
        {
            RuleFor(item => item.ProductId)
                .NotEmpty();

            RuleFor(item => item.Quantity)
                .GreaterThan(0);

            RuleFor(item => item.PurchasePrice)
                .GreaterThan(0);
        }
    }

    public class CreateSupplierOrderRequestDTOValidator : AbstractValidator<SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO>
    {
        public CreateSupplierOrderRequestDTOValidator()
        {
            RuleFor(request => request.SupplierId)
                .NotEmpty();

            RuleFor(request => request.WarehouseId)
                .NotEmpty();

            RuleFor(request => request.Items)
                .NotEmpty();

            RuleFor(request => request.Items)
                .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
                .WithMessage("Supplier order items must not contain duplicate products.");

            RuleForEach(request => request.Items)
                .SetValidator(new CreateSupplierOrderItemDTOValidator());
        }
    }

    public class GetSupplierOrderByIdRequestDTOValidator : AbstractValidator<SupplierOrderServiceDTOs.GetSupplierOrderByIdRequestDTO>
    {
        public GetSupplierOrderByIdRequestDTOValidator()
        {
            RuleFor(request => request.SupplierOrderId)
                .NotEmpty();
        }
    }

    public class SubmitSupplierOrderRequestDTOValidator : AbstractValidator<SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO>
    {
        public SubmitSupplierOrderRequestDTOValidator()
        {
            RuleFor(request => request.SupplierOrderId)
                .NotEmpty();
        }
    }

    public class ReceiveSupplierOrderItemDTOValidator : AbstractValidator<SupplierOrderServiceDTOs.ReceiveSupplierOrderItemDTO>
    {
        public ReceiveSupplierOrderItemDTOValidator()
        {
            RuleFor(item => item.SupplierOrderItemId)
                .NotEmpty();

            RuleFor(item => item.SalePrice)
                .GreaterThan(0);
        }
    }

    public class ReceiveSupplierOrderRequestDTOValidator : AbstractValidator<SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO>
    {
        public ReceiveSupplierOrderRequestDTOValidator()
        {
            RuleFor(request => request.SupplierOrderId)
                .NotEmpty();

            RuleFor(request => request.Items)
                .NotEmpty();

            RuleForEach(request => request.Items)
                .SetValidator(new ReceiveSupplierOrderItemDTOValidator());
        }
    }
}