using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace InventoryService.Tests.Integration.Fixtures
{
    // Captures the real app host's own log output (in particular ExceptionHandlingMiddleware's
    // "Unhandled exception" entries) so a failing integration test can print the actual
    // server-side exception. The HTTP response body never carries it - 500s always return a
    // fixed generic message by design - and the host's own console logging does not reliably
    // surface in CI's captured test output, so this reads it directly instead of guessing.
    public class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _entries = new();

        public IReadOnlyCollection<string> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(categoryName, _entries);
        }

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string categoryName, ConcurrentQueue<string> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= LogLevel.Warning;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                entries.Enqueue($"[{logLevel}] {categoryName}: {formatter(state, exception)}{(exception is null ? "" : $"\n{exception}")}");
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
