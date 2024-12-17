using System;
using System.Collections.Concurrent;
using GitHub.Runner.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Runner.Client
{
    public class JsonLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if(!IsEnabled(logLevel)) {
                return;
            }
            Console.WriteLine(JsonConvert.SerializeObject(new { level = "trace", msg = formatter(state, exception), time = DateTime.Now }));
        }
    }

    public class JsonLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, JsonLogger> _loggers = new (StringComparer.OrdinalIgnoreCase);
        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new JsonLogger());

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public sealed class Terminal : GitHub.Runner.Common.RunnerService, ITerminal
    {
        public bool Silent { get; set; }

        public event EventHandler CancelKeyPress;

        public void Dispose()
        {
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }

        public string ReadSecret()
        {
            throw new NotImplementedException();
        }

        public void Write(string message, ConsoleColor? colorCode = null)
        {
        }

        public void WriteError(Exception ex)
        {
        }

        public void WriteError(string line)
        {
        }

        public void WriteLine()
        {
        }

        public void WriteLine(string line, ConsoleColor? colorCode = null, bool skipTracing = false)
        {
        }

        public void WriteSection(string message)
        {
        }

        public void WriteSuccessMessage(string message)
        {
        }
    }
}