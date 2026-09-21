using FluentValidation;
using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Validators
{
    public class StockTransferLineDTOValidator : AbstractValidator<StockTransferServiceDTOs.StockTransferLineDTO>
    {
        public StockTransferLineDTOValidator()
        {
            RuleFor(line => line.StockItemId)
                .NotEmpty();

            RuleFor(line => line.BatchId)
                .NotEmpty();

            RuleFor(line => line.Quantity)
                .GreaterThan(0);
        }
    }

    public class InitiateTransferRequestDTOValidator : AbstractValidator<StockTransferServiceDTOs.InitiateTransferRequestDTO>
    {
        public InitiateTransferRequestDTOValidator()
        {
            RuleFor(request => request.DestinationWarehouseId)
                .NotEmpty();

            RuleFor(request => request.Items)
                .NotEmpty();

            RuleForEach(request => request.Items)
                .SetValidator(new StockTransferLineDTOValidator());
        }
    }

    public class GetShipmentByIdRequestDTOValidator : AbstractValidator<StockTransferServiceDTOs.GetShipmentByIdRequestDTO>
    {
        public GetShipmentByIdRequestDTOValidator()
        {
            RuleFor(request => request.ShipmentId)
                .NotEmpty();
        }
    }

    public class GetStockTransferByIdRequestDTOValidator : AbstractValidator<StockTransferServiceDTOs.GetStockTransferByIdRequestDTO>
    {
        public GetStockTransferByIdRequestDTOValidator()
        {
            RuleFor(request => request.StockTransferId)
                .NotEmpty();
        }
    }

    public class ReceiveTransferRequestDTOValidator : AbstractValidator<StockTransferServiceDTOs.ReceiveTransferRequestDTO>
    {
        public ReceiveTransferRequestDTOValidator()
        {
            RuleFor(request => request.StockTransferId)
                .NotEmpty();

            RuleFor(request => request.Quantity)
                .GreaterThan(0);
        }
    }

    public class CancelTransferRequestDTOValidator : AbstractValidator<StockTransferServiceDTOs.CancelTransferRequestDTO>
    {
        public CancelTransferRequestDTOValidator()
        {
            RuleFor(request => request.StockTransferId)
                .NotEmpty();
        }
    }
}