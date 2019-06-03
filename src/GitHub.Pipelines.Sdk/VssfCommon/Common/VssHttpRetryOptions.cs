using GitHub.Services.Common.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Defines the options used for configuring the retry policy.
    /// </summary>
    public class VssHttpRetryOptions
    {
        public VssHttpRetryOptions()
            : this (new VssHttpRetryableStatusCodeFilter[] { s_hostShutdownFilter } )
        {
        }

        public VssHttpRetryOptions(IEnumerable<VssHttpRetryableStatusCodeFilter> filters)
        {
            this.BackoffCoefficient = s_backoffCoefficient;
            this.MinBackoff = s_minBackoff;
            this.MaxBackoff = s_maxBackoff;
            this.MaxRetries = 5;
            this.RetryableStatusCodes = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.ServiceUnavailable,
            };

            this.m_retryFilters = new HashSet<VssHttpRetryableStatusCodeFilter>(filters);
        }

        /// <summary>
        /// Gets a singleton read-only instance of the default settings.
        /// </summary>
        public static VssHttpRetryOptions Default
        {
            get
            {
                return s_defaultOptions.Value;
            }
        }

        /// <summary>
        /// Gets or sets the coefficient which exponentially increases the backoff starting at <see cref="MinBackoff" />.
        /// </summary>
        public TimeSpan BackoffCoefficient
        {
            get
            {
                return m_backoffCoefficient;
            }
            set
            {
                ThrowIfReadonly();
                m_backoffCoefficient = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum backoff interval to be used.
        /// </summary>
        public TimeSpan MinBackoff
        {
            get
            {
                return m_minBackoff;
            }
            set
            {
                ThrowIfReadonly();
                m_minBackoff = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum backoff interval to be used.
        /// </summary>
        public TimeSpan MaxBackoff
        {
            get
            {
                return m_maxBackoff;
            }
            set
            {
                ThrowIfReadonly();
                m_maxBackoff = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of retries allowed.
        /// </summary>
        public Int32 MaxRetries
        {
            get
            {
                return m_maxRetries;
            }
            set
            {
                ThrowIfReadonly();
                m_maxRetries = value;
            }
        }

        /// <summary>
        /// Gets a set of HTTP status codes which should be retried.
        /// </summary>
        public ICollection<HttpStatusCode> RetryableStatusCodes
        {
            get
            {
                return m_retryableStatusCodes;
            }
            private set
            {
                ThrowIfReadonly();
                m_retryableStatusCodes = value;
            }
        }

        /// <summary>
        /// How to verify that the response can be retried.
        /// </summary>
        /// <param name="response">Response message from a request</param>
        /// <returns>True if the request can be retried, false otherwise.</returns>
        public Boolean IsRetryableResponse(HttpResponseMessage response)
        {
            if (m_retryableStatusCodes.Contains(response.StatusCode))
            {
                foreach (VssHttpRetryableStatusCodeFilter filter in m_retryFilters)
                {
                    if (filter(response))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures that no further modifications may be made to the retry options.
        /// </summary>
        /// <returns>A read-only instance of the retry options</returns>
        public VssHttpRetryOptions MakeReadonly()
        {
            if (Interlocked.CompareExchange(ref m_isReadOnly, 1, 0) == 0)
            {
                m_retryableStatusCodes = new ReadOnlyCollection<HttpStatusCode>(m_retryableStatusCodes.ToList());
                m_retryFilters = new ReadOnlyCollection<VssHttpRetryableStatusCodeFilter>(m_retryFilters.ToList());
            }
            return this;
        }



        /// <summary>
        /// Throws an InvalidOperationException if this is marked as ReadOnly.
        /// </summary>
        private void ThrowIfReadonly()
        {
            if (m_isReadOnly > 0)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Returns false if we should continue retrying based on the response, and true if we should not, even though
        /// this is technically a retryable status code.
        /// </summary>
        /// <param name="response">The response to check if we should retry the request.</param>
        /// <returns>False if we should retry, true if we should not based on the response.</returns>
        public delegate Boolean VssHttpRetryableStatusCodeFilter(HttpResponseMessage response);

        private Int32 m_isReadOnly;
        private Int32 m_maxRetries;
        private TimeSpan m_minBackoff;
        private TimeSpan m_maxBackoff;
        private TimeSpan m_backoffCoefficient;
        private ICollection<HttpStatusCode> m_retryableStatusCodes;
        private ICollection<VssHttpRetryableStatusCodeFilter> m_retryFilters;
        private static TimeSpan s_minBackoff = TimeSpan.FromSeconds(10);
        private static TimeSpan s_maxBackoff = TimeSpan.FromMinutes(10);
        private static TimeSpan s_backoffCoefficient = TimeSpan.FromSeconds(1);
        private static Lazy<VssHttpRetryOptions> s_defaultOptions = new Lazy<VssHttpRetryOptions>(() => new VssHttpRetryOptions().MakeReadonly());
        private static VssHttpRetryableStatusCodeFilter s_hostShutdownFilter = new VssHttpRetryableStatusCodeFilter(response => response.Headers.Contains(HttpHeaders.VssHostOfflineError));
    }
}
