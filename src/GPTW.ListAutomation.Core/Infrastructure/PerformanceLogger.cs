using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GPTW.ListAutomation.Core.Infrastructure
{
    public sealed class PerformanceLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _tag;
        private readonly object _logStatementItems;
        private readonly LogLevel _logLevel;
        private readonly Stopwatch _sw;

        public PerformanceLogger(ILogger logger, string tag, dynamic logStatementItems = null, LogLevel logLevel = LogLevel.Information)
        {
            _logger = logger;
            _tag = tag;
            _logStatementItems = logStatementItems ?? new { };
            _logLevel = logLevel;
            _sw = new Stopwatch();
        }

        public PerformanceLogger Start()
        {
            _sw.Start();
            return this;
        }

        private PerformanceLogger Stop()
        {
            _sw.Stop();
            return this;
        }

        private void Log()
        {
            try
            {
                var items = _logStatementItems.GetType().GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(_logStatementItems));

                var keys =
                    new[] { "{Duration}" }
                    .Concat(items
                        .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                        .Select(kv => "{" + kv.Key + "}"));

                //var elapsedTime = _sw.ElapsedMilliseconds;
                var ts = _sw.Elapsed;
                var elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);

                var objects =
                    new object[] { elapsedTime }
                    .Concat(items
                        .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                        .Select(kv => kv.Value));

                var template = $"[{_tag}] -- {string.Join(" ", keys)}";

                _logger.Log(_logLevel, template, objects.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to log performance {@Exception}", ex);
            }
        }

        public void Dispose()
        {
            Stop();
            Log();
        }
    }

    public static class PerformanceLoggerExtensions
    {
        public static PerformanceLogger MeasureDatabaseRead(this ILogger logger, string repository, string function)
        {
            return new PerformanceLogger(logger, "DBPerf", new { Operation = "Read", Repository = repository, Function = function }).Start();
        }

        public static PerformanceLogger MeasureDatabaseWrite(this ILogger logger, string repository, string function)
        {
            return new PerformanceLogger(logger, "DBPerf", new { Operation = "Write", Repository = repository, Function = function }).Start();
        }

        public static PerformanceLogger MeasureApiServiceOperation(this ILogger logger, string service, string function)
        {
            return new PerformanceLogger(logger, "ApiCall", new { Operation = "Invoke", Service = service, Function = function }).Start();
        }

        public static PerformanceLogger MeasureOperation(this ILogger logger, string service, string function)
        {
            return new PerformanceLogger(logger, "Operation", new { Operation = "Compute", Service = service, Function = function }).Start();
        }

        public static PerformanceLogger MeasureDatabaseRead<T>(this ILogger<T> logger, string function)
        {
            return new PerformanceLogger(logger, "DBPerf", new { Operation = "Read", Repository = typeof(T).Name, Function = function }).Start();
        }

        public static PerformanceLogger MeasureDatabaseWrite<T>(this ILogger<T> logger, string function)
        {
            return new PerformanceLogger(logger, "DBPerf", new { Operation = "Write", Repository = typeof(T).Name, Function = function }).Start();
        }

        public static PerformanceLogger MeasureApiServiceOperation<T>(this ILogger<T> logger, string function)
        {
            return new PerformanceLogger(logger, "ApiCall", new { Operation = "Invoke", Service = typeof(T).Name, Function = function }).Start();
        }

        public static PerformanceLogger MeasureOperation<T>(this ILogger<T> logger, string function)
        {
            return new PerformanceLogger(logger, "Operation", new { Operation = "Compute", Service = typeof(T).Name, Function = function }).Start();
        }
    }
}
