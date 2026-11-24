using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Keeps every log entry the API writes, with the values of its scopes (such as the request's TraceId), so tests can
/// check what is logged and that no secret is.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public IReadOnlyList<LogEntry> Entries => _entries.ToList();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries, () => _scopes);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries, Func<IExternalScopeProvider> scopes) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopes().Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scopeValues = new Dictionary<string, string?>();
            scopes().ForEachScope((scope, values) =>
            {
                foreach (var (key, value) in NamedValues(scope))
                {
                    values[key] = value;
                }
            }, scopeValues);

            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception), NamedValues(state), scopeValues));
        }

        private static Dictionary<string, string?> NamedValues(object? state) =>
            (state as IEnumerable<KeyValuePair<string, object?>> ?? [])
                .Where(v => v.Key != "{OriginalFormat}")
                .GroupBy(v => v.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value?.ToString());
    }
}

/// <summary>
/// One captured log entry: its category, level, rendered message, named values and the values of its scopes.
/// </summary>
public sealed record LogEntry(
    string Category,
    LogLevel Level,
    string Message,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyDictionary<string, string?> Scopes);
