using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class CreateProductRequestDTOValidator : AbstractValidator<ProductServiceDTOs.CreateProductRequestDTO>
    {
        public CreateProductRequestDTOValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty();

            RuleFor(request => request.ProductCategory)
                .NotEmpty();
        }
    }

    public class GetProductByIdRequestDTOValidator : AbstractValidator<ProductServiceDTOs.GetProductByIdRequestDTO>
    {
        public GetProductByIdRequestDTOValidator()
        {
            RuleFor(request => request.ProductId)
                .NotEmpty();
        }
    }

    public class UpdateProductRequestDTOValidator : AbstractValidator<ProductServiceDTOs.UpdateProductRequestDTO>
    {
        public UpdateProductRequestDTOValidator()
        {
            RuleFor(request => request.ProductId)
                .NotEmpty();

            RuleFor(request => request.Name)
                .NotEmpty();

            RuleFor(request => request.ProductCategory)
                .NotEmpty();
        }
    }

    public class DeleteProductRequestDTOValidator : AbstractValidator<ProductServiceDTOs.DeleteProductRequestDTO>
    {
        public DeleteProductRequestDTOValidator()
        {
            RuleFor(request => request.ProductId)
                .NotEmpty();
        }
    }
}