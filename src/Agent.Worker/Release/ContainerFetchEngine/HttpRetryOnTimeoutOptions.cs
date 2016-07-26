using System;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    /// <summary>
    /// Defines the options used for configuring the retry policy.
    /// </summary>
    public class HttpRetryOnTimeoutOptions
    {
        private Int32 _isReadOnly;
        private Int32 _maxRetries;
        private TimeSpan _minBackoff;
        private TimeSpan _maxBackoff;
        private TimeSpan _backoffCoefficient;
        private static readonly TimeSpan _defaultMinBackoff = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _defaultMaxBackoff = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan _defaultBackoffCoefficient = TimeSpan.FromSeconds(1);
        private static readonly Lazy<HttpRetryOnTimeoutOptions> _defaultOptions = new Lazy<HttpRetryOnTimeoutOptions>(() => new HttpRetryOnTimeoutOptions().MakeReadonly());

        public HttpRetryOnTimeoutOptions()
        {
            BackoffCoefficient = _defaultBackoffCoefficient;
            MinBackoff = _defaultMinBackoff;
            MaxBackoff = _defaultMaxBackoff;
            MaxRetries = 5;
        }

        /// <summary>
        /// Gets a singleton read-only instance of the default settings.
        /// </summary>
        public static HttpRetryOnTimeoutOptions Default
        {
            get
            {
                return _defaultOptions.Value;
            }
        }

        /// <summary>
        /// Gets or sets the coefficient which exponentially increases the backoff starting at <see cref="MinBackoff" />.
        /// </summary>
        public TimeSpan BackoffCoefficient
        {
            get
            {
                return _backoffCoefficient;
            }
            set
            {
                ThrowIfReadonly();
                _backoffCoefficient = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum backoff interval to be used.
        /// </summary>
        public TimeSpan MinBackoff
        {
            get
            {
                return _minBackoff;
            }
            set
            {
                ThrowIfReadonly();
                _minBackoff = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum backoff interval to be used.
        /// </summary>
        public TimeSpan MaxBackoff
        {
            get
            {
                return _maxBackoff;
            }
            set
            {
                ThrowIfReadonly();
                _maxBackoff = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of retries allowed.
        /// </summary>
        public Int32 MaxRetries
        {
            get
            {
                return _maxRetries;
            }
            set
            {
                ThrowIfReadonly();
                _maxRetries = value;
            }
        }

        /// <summary>
        /// Ensures that no further modifications may be made to the retry options.
        /// </summary>
        /// <returns>A read-only instance of the retry options</returns>
        public HttpRetryOnTimeoutOptions MakeReadonly()
        {
            if (Interlocked.CompareExchange(ref _isReadOnly, 1, 0) == 0)
            {
                // Make any lists read-only here.
            }

            return this;
        }

        /// <summary>
        /// Throws an InvalidOperationException if this is marked as ReadOnly.
        /// </summary>
        private void ThrowIfReadonly()
        {
            if (_isReadOnly > 0)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
