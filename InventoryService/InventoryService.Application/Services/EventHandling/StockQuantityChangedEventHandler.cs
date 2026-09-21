using InventoryService.Application.Services.Interfaces;
using Shared.Contracts.Events;
using System.Text.Json;

namespace InventoryService.Application.Services.EventHandling
{
    public class StockQuantityChangedEventHandler : IInventoryEventHandler
    {
        public string NameKey
        {
            get
            {
                return nameof(StockQuantityChangedEvent);
            }
        }

        private readonly ILowStockAlertService _lowStockAlertService;

        public StockQuantityChangedEventHandler(ILowStockAlertService lowStockAlertService)
        {
            _lowStockAlertService = lowStockAlertService;
        }

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var stockQuantityChangedEvent = JsonSerializer.Deserialize<StockQuantityChangedEvent>(payload)
                ?? throw new InvalidOperationException($"Empty {nameof(StockQuantityChangedEvent)} payload.");

            if (stockQuantityChangedEvent.Increased)
            {
                await _lowStockAlertService.EvaluateAfterIncreaseAsync(
                    stockQuantityChangedEvent.ProductId, stockQuantityChangedEvent.WarehouseId, cancellationToken);
            }
            else
            {
                await _lowStockAlertService.EvaluateAfterDecreaseAsync(
                    stockQuantityChangedEvent.ProductId, stockQuantityChangedEvent.WarehouseId, cancellationToken);
            }
        }
    }
}
