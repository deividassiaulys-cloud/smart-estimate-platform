using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Keeps every log entry the API writes, so tests can check what is logged and that no secret is.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries.ToList();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var values = state as IEnumerable<KeyValuePair<string, object?>> ?? [];
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception),
                values.Where(v => v.Key != "{OriginalFormat}").ToDictionary(v => v.Key, v => v.Value?.ToString())));
        }
    }
}

/// <summary>
/// One captured log entry: its category, level, rendered message and named values.
/// </summary>
public sealed record LogEntry(string Category, LogLevel Level, string Message, IReadOnlyDictionary<string, string?> Values);
