using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class CreateStockThresholdRequestDTOValidator : AbstractValidator<StockServiceDTOs.CreateStockThresholdRequestDTO>
    {
        public CreateStockThresholdRequestDTOValidator()
        {
            RuleFor(request => request.ProductId)
                .NotEmpty();

            RuleFor(request => request.WarehouseId)
                .NotEmpty();

            RuleFor(request => request.ReorderLevel)
                .GreaterThanOrEqualTo(0);

            RuleFor(request => request.ReorderQuantity)
                .GreaterThan(0);
        }
    }

    public class GetStockThresholdByIdRequestDTOValidator : AbstractValidator<StockServiceDTOs.GetStockThresholdByIdRequestDTO>
    {
        public GetStockThresholdByIdRequestDTOValidator()
        {
            RuleFor(request => request.StockThresholdId)
                .NotEmpty();
        }
    }

    public class UpdateStockThresholdRequestDTOValidator : AbstractValidator<StockServiceDTOs.UpdateStockThresholdRequestDTO>
    {
        public UpdateStockThresholdRequestDTOValidator()
        {
            RuleFor(request => request.StockThresholdId)
                .NotEmpty();

            RuleFor(request => request.ReorderLevel)
                .GreaterThanOrEqualTo(0);

            RuleFor(request => request.ReorderQuantity)
                .GreaterThan(0);
        }
    }

    public class DeleteStockThresholdRequestDTOValidator : AbstractValidator<StockServiceDTOs.DeleteStockThresholdRequestDTO>
    {
        public DeleteStockThresholdRequestDTOValidator()
        {
            RuleFor(request => request.StockThresholdId)
                .NotEmpty();
        }
    }

    public class ReceiveStockRequestDTOValidator : AbstractValidator<StockServiceDTOs.ReceiveStockRequestDTO>
    {
        public ReceiveStockRequestDTOValidator()
        {
            RuleFor(request => request.ProductId)
                .NotEmpty();

            RuleFor(request => request.WarehouseId)
                .NotEmpty();

            RuleFor(request => request.Quantity)
                .GreaterThan(0);

            RuleFor(request => request.Price)
                .GreaterThan(0);
        }
    }

    public class GetStockItemByIdRequestDTOValidator : AbstractValidator<StockServiceDTOs.GetStockItemByIdRequestDTO>
    {
        public GetStockItemByIdRequestDTOValidator()
        {
            RuleFor(request => request.StockItemId)
                .NotEmpty();
        }
    }

    public class SellStockRequestDTOValidator : AbstractValidator<StockServiceDTOs.SellStockRequestDTO>
    {
        public SellStockRequestDTOValidator()
        {
            RuleFor(request => request.StockItemId)
                .NotEmpty();

            RuleFor(request => request.Quantity)
                .GreaterThan(0);
        }
    }

    public class AdjustStockRequestDTOValidator : AbstractValidator<StockServiceDTOs.AdjustStockRequestDTO>
    {
        public AdjustStockRequestDTOValidator()
        {
            RuleFor(request => request.StockItemId)
                .NotEmpty();

            RuleFor(request => request.Quantity)
                .GreaterThan(0);
        }
    }

    public class GetMovementsByStockItemIdRequestDTOValidator : AbstractValidator<StockServiceDTOs.GetMovementsByStockItemIdRequestDTO>
    {
        public GetMovementsByStockItemIdRequestDTOValidator()
        {
            RuleFor(request => request.StockItemId)
                .NotEmpty();
        }
    }
}