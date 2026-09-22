using Microsoft.AspNetCore.SignalR.Client;

namespace InventoryService.Tests.Integration.Fixtures
{
    public static class HubConnectionRetryExtensions
    {
        // SignalR's Redis backplane (AddStackExchangeRedis) opens its own Redis subscription in
        // the background and isn't guaranteed to be ready the instant the app host reports itself
        // started - a hub method that touches group membership (like JoinWarehouse, via
        // Groups.AddToGroupAsync) can fail on a connection made immediately after StartAsync,
        // especially on a slower CI runner. Retrying the first invocation absorbs that warm-up
        // window without masking a persistent failure - a real bug still fails after every attempt.
        public static async Task InvokeWithRetryAsync(
            this HubConnection connection, string methodName, object arg, int maxAttempts = 5)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await connection.InvokeAsync(methodName, arg);
                    return;
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
            }
        }
    }
}
