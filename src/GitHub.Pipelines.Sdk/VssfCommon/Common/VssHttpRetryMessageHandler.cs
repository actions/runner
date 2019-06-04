using GitHub.Services.Common.Diagnostics;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Handles automatic replay of HTTP requests when errors are encountered based on a configurable set of options.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VssHttpRetryMessageHandler : DelegatingHandler
    {
        public VssHttpRetryMessageHandler(Int32 maxRetries)
            : this(new VssHttpRetryOptions { MaxRetries = maxRetries })
        {
        }

        public VssHttpRetryMessageHandler(Int32 maxRetries, string clientName)
        : this(new VssHttpRetryOptions { MaxRetries = maxRetries })
        {
            m_clientName = clientName;
        }

        public VssHttpRetryMessageHandler(VssHttpRetryOptions options)
        {
            m_retryOptions = options;
        }

        public VssHttpRetryMessageHandler(
            VssHttpRetryOptions options, 
            HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            m_retryOptions = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Int32 attempt = 1;
            HttpResponseMessage response = null;
            HttpRequestException exception = null;
            VssTraceActivity traceActivity = VssTraceActivity.Current;

            // Allow overriding default retry options per request
            VssHttpRetryOptions retryOptions = m_retryOptions;
            object retryOptionsObject;
            if (request.Properties.TryGetValue(HttpRetryOptionsKey, out retryOptionsObject)) // NETSTANDARD compliant, TryGetValue<T> is not
            {
                // Fallback to default options if object of unexpected type was passed
                retryOptions = retryOptionsObject as VssHttpRetryOptions ?? m_retryOptions;
            }

            TimeSpan minBackoff = retryOptions.MinBackoff;
            Int32 maxAttempts = retryOptions.MaxRetries + 1;

            IVssHttpRetryInfo retryInfo = null;
            object retryInfoObject;
            if (request.Properties.TryGetValue(HttpRetryInfoKey, out retryInfoObject)) // NETSTANDARD compliant, TryGetValue<T> is not
            {
                retryInfo = retryInfoObject as IVssHttpRetryInfo;
            }

            if (IsLowPriority(request))
            {
                // Increase the backoff and retry count, low priority requests can be retried many times if the server is busy.
                minBackoff = TimeSpan.FromSeconds(minBackoff.TotalSeconds * 2);
                maxAttempts = maxAttempts * 10;
            }

            TimeSpan backoff = minBackoff;

            while (attempt <= maxAttempts)
            {
                // Reset the exception so we don't have a lingering variable
                exception = null;

                Boolean canRetry = false;
                SocketError? socketError = null;
                HttpStatusCode? statusCode = null;
                WebExceptionStatus? webExceptionStatus = null;
                WinHttpErrorCode? winHttpErrorCode = null;
                CurlErrorCode? curlErrorCode = null;
                string afdRefInfo = null;
                try
                {
                    if (attempt == 1)
                    {
                        retryInfo?.InitialAttempt(request);
                    }

                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (attempt > 1)
                    {
                        TraceHttpRequestSucceededWithRetry(traceActivity, response, attempt);
                    }

                    // Verify the response is successful or the status code is one that may be retried. 
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    else
                    {
                        statusCode = response.StatusCode;
                        afdRefInfo = response.Headers.TryGetValues(HttpHeaders.AfdResponseRef, out var headers) ? headers.First() : null;
                        canRetry = m_retryOptions.IsRetryableResponse(response);
                    }
                }
                catch (HttpRequestException ex)
                {
                    exception = ex;
                    canRetry = VssNetworkHelper.IsTransientNetworkException(exception, m_retryOptions, out statusCode, out webExceptionStatus, out socketError, out winHttpErrorCode, out curlErrorCode);
                }
                catch (TimeoutException)
                {
                    throw;
                }

                if (attempt < maxAttempts && canRetry)
                {
                    backoff = BackoffTimerHelper.GetExponentialBackoff(attempt, minBackoff, m_retryOptions.MaxBackoff, m_retryOptions.BackoffCoefficient);
                    retryInfo?.Retry(backoff);
                    TraceHttpRequestRetrying(traceActivity, request, attempt, backoff, statusCode, webExceptionStatus, socketError, winHttpErrorCode, curlErrorCode, afdRefInfo);
                }
                else
                {
                    if (attempt < maxAttempts)
                    {
                        if (exception == null)
                        {
                            TraceHttpRequestFailed(traceActivity, request, statusCode != null ? statusCode.Value : (HttpStatusCode)0, afdRefInfo);
                        }
                        else
                        {
                            TraceHttpRequestFailed(traceActivity, request, exception);
                        }
                    }
                    else
                    {
                        TraceHttpRequestFailedMaxAttempts(traceActivity, request, attempt, statusCode, webExceptionStatus, socketError, winHttpErrorCode, curlErrorCode, afdRefInfo);
                    }
                    break;
                }

                // Make sure to dispose of this so we don't keep the connection open
                if (response != null)
                {
                    response.Dispose();
                }

                attempt++;
                TraceRaw(request, 100011, TraceLevel.Error,
                    "{{ \"Client\":\"{0}\", \"Endpoint\":\"{1}\", \"Attempt\":{2}, \"MaxAttempts\":{3}, \"Backoff\":{4} }}",
                    m_clientName,
                    request.RequestUri.Host,
                    attempt,
                    maxAttempts,
                    backoff.TotalMilliseconds);
                await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
            }

            if (exception != null)
            {
                throw exception;
            }

            return response;
        }

        protected virtual void TraceRaw(HttpRequestMessage request, int tracepoint, TraceLevel level, string message, params object[] args)
        {
            // implement in Server so retries are recorded in ProductTrace
        }
        
        protected virtual void TraceHttpRequestFailed(VssTraceActivity activity, HttpRequestMessage request, HttpStatusCode statusCode, string afdRefInfo)
        {
            VssHttpEventSource.Log.HttpRequestFailed(activity, request, statusCode, afdRefInfo);
        }

        protected virtual void TraceHttpRequestFailed(VssTraceActivity activity, HttpRequestMessage request, Exception exception)
        {
            VssHttpEventSource.Log.HttpRequestFailed(activity, request, exception);
        }

        protected virtual void TraceHttpRequestFailedMaxAttempts(VssTraceActivity activity, HttpRequestMessage request, Int32 attempt, HttpStatusCode? httpStatusCode, WebExceptionStatus? webExceptionStatus, SocketError? socketErrorCode, WinHttpErrorCode? winHttpErrorCode, CurlErrorCode? curlErrorCode, string afdRefInfo)
        {
            VssHttpEventSource.Log.HttpRequestFailedMaxAttempts(activity, request, attempt, httpStatusCode, webExceptionStatus, socketErrorCode, winHttpErrorCode, curlErrorCode, afdRefInfo);
        }

        protected virtual void TraceHttpRequestSucceededWithRetry(VssTraceActivity activity, HttpResponseMessage response, Int32 attempt)
        {
            VssHttpEventSource.Log.HttpRequestSucceededWithRetry(activity, response, attempt);
        }

        protected virtual void TraceHttpRequestRetrying(VssTraceActivity activity, HttpRequestMessage request, Int32 attempt, TimeSpan backoffDuration, HttpStatusCode? httpStatusCode, WebExceptionStatus? webExceptionStatus, SocketError? socketErrorCode, WinHttpErrorCode? winHttpErrorCode, CurlErrorCode? curlErrorCode, string afdRefInfo)
        {
            VssHttpEventSource.Log.HttpRequestRetrying(activity, request, attempt, backoffDuration, httpStatusCode, webExceptionStatus, socketErrorCode, winHttpErrorCode, curlErrorCode, afdRefInfo);
        }

        private static bool IsLowPriority(HttpRequestMessage request)
        {
            bool isLowPriority = false;
            
            IEnumerable<string> headers;

            if (request.Headers.TryGetValues(HttpHeaders.VssRequestPriority, out headers) && headers != null)
            {
                string header = headers.FirstOrDefault();
                isLowPriority = string.Equals(header, "Low", StringComparison.OrdinalIgnoreCase);
            }

            return isLowPriority;
        }

        private VssHttpRetryOptions m_retryOptions;
        public const string HttpRetryInfoKey = "HttpRetryInfo";
        public const string HttpRetryOptionsKey = "VssHttpRetryOptions";
        private string m_clientName = "";
    }
}
