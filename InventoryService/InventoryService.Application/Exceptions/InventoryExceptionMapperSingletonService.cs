using Shared.Kernel.Exceptions;
using System.Net;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Application.Exceptions
{
    public class InventoryExceptionMapperSingletonService : IExceptionMapperService
    {
        public HttpStatusCode? Map(Exception exception)
        {
            return exception switch
            {
                InvalidWorkingHoursException => HttpStatusCode.BadRequest,
                NegativeValueException => HttpStatusCode.BadRequest,
                SupplierOrderPartialReceiptNotSupportedException => HttpStatusCode.BadRequest,
                WarehouseNameAlreadyTakenException => HttpStatusCode.Conflict,
                StockTransferNotInTransitException => HttpStatusCode.Conflict,
                SupplierOrderNotCreatedException => HttpStatusCode.Conflict,
                SupplierOrderNotSubmittedException => HttpStatusCode.Conflict,
                LowStockAlertAlreadyResolvedException => HttpStatusCode.Conflict,
                StockThresholdAlreadyExistsException => HttpStatusCode.Conflict,
                InsufficientStockAvailableException => HttpStatusCode.Conflict,
                StockItemPriceMismatchException => HttpStatusCode.Conflict,
                StockItemBatchMismatchException => HttpStatusCode.Conflict,
                ShipmentInitiationFailedException => HttpStatusCode.Conflict,
                _ => null
            };
        }
    }
}