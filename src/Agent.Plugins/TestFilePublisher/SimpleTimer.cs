using System;
using System.Diagnostics;
using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestFilePublisher
{
    /// <summary>
    /// This is a utitily class used for recording timing
    /// information. Its usage is 
    /// using (SimpleTimer timer = new SimpleTimer("MyOperation"))
    /// {
    ///     MyOperation...
    /// }
    /// </summary>
    public class SimpleTimer : IDisposable
    {
        /// <summary>
        /// Creates a timer with threshold. A perf message is logged only if
        /// the time elapsed is more than the threshold.
        /// </summary>
        public SimpleTimer(string timerName, ITraceLogger logger, TimeSpan threshold, TelemetryDataWrapper telemetryWrapper)
        {
            _name = timerName;
            _logger = logger;
            _threshold = threshold;
            _telemetryWrapper = telemetryWrapper;
            _timer = Stopwatch.StartNew();
        }

        /// <summary>
        /// Implement IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Stop the watch and log the trace message with the elapsed time.
        /// Additionaly also adds the elapsed time to telemetry under the timer nam
        /// </summary>
        public void StopAndLog()
        {
            _timer.Stop();

            _telemetryWrapper.AddAndAggregate(_timer.Elapsed.TotalMilliseconds);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                StopAndLog();
            }

            _disposed = true;
        }

        #region private variables.

        private bool _disposed;
        private ITraceLogger _logger;
        private TelemetryDataWrapper _telemetryWrapper;
        private readonly Stopwatch _timer;
        private readonly string _name;
        private readonly TimeSpan _threshold;

        #endregion
    }
}
