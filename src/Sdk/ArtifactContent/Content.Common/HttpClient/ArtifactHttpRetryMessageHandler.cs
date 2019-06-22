using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.Content.Common.Tracing;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Extends VssHttpRetryMessageHandler to provide an option to retry timeouts as well.
    /// </summary>
    /// <remarks>
    /// CodeSync: Logic heavily lifted from VssHttpRetryMessageHandler base class which, unfortunately, wasn't quite 
    /// customizable enough to allow retrying of custom exceptions and status codes that don't derive from HttpRequestException (i.e. TimeoutException)
    /// Regarding Vss request settings:
    /// 1) Do use VssHttpRequestSettings or VssHttpRetryOptions, which are defined in GitHub.Services.Common.
    /// 2) Do NOT use VssClientHttpRequestSettings because:
    /// 2.1) VssClientHttpRequestSettings is defined as EditorBrowsableState.Never, which implies it's internal/legacy.
    /// 2.2) VssClientHttpRequestSettings is defined in the Vssf.InteractiveClient module, which we only want to depend on client-side (e.g. AppShared).
    /// </remarks>
    public class ArtifactHttpRetryMessageHandler : VssHttpRetryMessageHandler
    {
        internal static readonly VssHttpRetryOptions DefaultRetryOptions;

        static ArtifactHttpRetryMessageHandler()
        {
            // Added the below retry HTTP statuscodes to make the logic consistent with the retry options that occur due to exceptions.
            // Tested the same with Fiddler using auto-responder by simulating the below statuscode responses and observed that retry does occur as expected.
            DefaultRetryOptions = new VssHttpRetryOptions();
            DefaultRetryOptions.RetryableStatusCodes.AddRange(
                new HashSet<HttpStatusCode> {
                    HttpStatusCode.RequestTimeout,
                    VssNetworkHelper.TooManyRequests,
                    HttpStatusCode.InternalServerError,
                });
            DefaultRetryOptions.MakeReadonly();
        }

        private readonly IAppTraceSource tracer;
        private readonly Func<int, TimeSpan, TimeSpan, TimeSpan, TimeSpan> GetExponentialBackoff;
        private readonly VssHttpRetryOptions retryOptions;

        public ArtifactHttpRetryMessageHandler(IAppTraceSource tracer)
            : this(tracer, options: null, getExponentialBackoff: null)
        {
        }

        internal ArtifactHttpRetryMessageHandler(IAppTraceSource tracer, VssHttpRetryOptions options = null, Func<int, TimeSpan, TimeSpan, TimeSpan, TimeSpan> getExponentialBackoff = null)
            : base(options ?? DefaultRetryOptions)
        {
            ArgumentUtility.CheckForNull(tracer, nameof(tracer));
            this.tracer = tracer;

            this.retryOptions = options ?? DefaultRetryOptions;

            this.GetExponentialBackoff = getExponentialBackoff ?? BackoffTimerHelper.GetExponentialBackoff;
        }

        internal static HttpClient CreateHttpClientWithRetryHandler(IAppTraceSource tracer, ArtifactHttpRetryMessageHandler retryHandler)
        {
            // The default handler for "new HttpClient()"
            // If we were using VssHttpClientBase, then this would be: new VssHttpMessageHandler(credentials, settings ?? new VssHttpRequestSettings()); 
            var innerHandler = new HttpClientHandler();

            // The AS retry handler
            if (retryHandler == null)
            {
                retryHandler = new ArtifactHttpRetryMessageHandler(options: DefaultRetryOptions, tracer: tracer);
            }
            else
            {
                retryHandler.retryOptions.RetryableStatusCodes.AddRange(DefaultRetryOptions.RetryableStatusCodes);
            }

            // Wire in the retry handler
            var handlers = new DelegatingHandler[] { retryHandler };
            var disposeHandler = true; // Same as VssHttpClientBase
            var pipeline = CreatePipeline(innerHandler, handlers);
            var client = new HttpClient(pipeline, disposeHandler);

            // Disable the HttpClient timeout since we handle it ourselves
            client.Timeout = TimeSpan.FromMilliseconds(-1.0);

            // VssHttpClientBase sets the BaseAddress, but we omit that because the retry handlers don't use it
            // and the scenario for this constructor is where the caller expects a plain-as-possible HttpClient.
            // client.BaseAddress = baseUrl;

            return client;
        }

        /// <summary>
        /// Returns a plain HttpClient with the vss-based retry DelegatingHandler used by the rest of the
        /// code by applying similar logic from VssHttpClientBase constructor and VssHttpClientBase.BuildHandler
        /// </summary>
        public static HttpClient CreateHttpClientWithRetryHandler(IAppTraceSource tracer)
        {
            return CreateHttpClientWithRetryHandler(tracer, retryHandler: null);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Int32 attempt = 1;
            HttpResponseMessage response = null;
            Exception exception = null;
            TimeSpan backoff = retryOptions.MinBackoff;
            Int32 maxAttempts = (retryOptions.MaxRetries > 0 ? retryOptions.MaxRetries : 0) + 1;
            Boolean canRetryOnSocketException = string.Equals(Environment.GetEnvironmentVariable("VSO_AS_HTTP_CanRetryOnSocketException"), bool.TrueString, System.StringComparison.OrdinalIgnoreCase);
            Boolean canRetry = false;

            do
            {
                // Reset the exception so we don't have a lingering variable
                exception = null;
                canRetry = false;
                SocketError? socketError = null;
                HttpStatusCode? statusCode = null;
                WebExceptionStatus? webExceptionStatus = null;
                WinHttpErrorCode? winHttpErrorCode = null;
                CurlErrorCode? curlErrorCode = null;

                try
                {
                    // Note the base implementation traces to 2 places by default:
                    // 1) TraceRaw for each retry attempt which we override below in order to trace to IAppTraceSource.
                    // 2) VssHttpEventSource.Log, which logs details about the failed request and retry logic to event source "Microsoft-VSS-Http". VssHttpMessageHandler also logs there.
                    response = await GetResponseMessage(request, cancellationToken);

                    // Verify the response is successful or the status code is one that may be retried. 
                    if (!response.IsSuccessStatusCode)
                    {
                        statusCode = response.StatusCode;
                        canRetry = retryOptions.IsRetryableResponse(response);

                        tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(SendAsync)}: {request.RequestUri} attempt {attempt}/{maxAttempts} failed with {nameof(response.StatusCode)} {statusCode}, {nameof(retryOptions.IsRetryableResponse)} {canRetry}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    ex.SetHttpMessagesForTracing(request, response);
                    exception = ex;
                    // We do not retry on IOException here. The HTTP handler will successfully return once it receives the full headers.
                    // The IOException, which commonly manifests during a broken TCP transmission, will not have a chance to be thrown
                    // until after the client is reading the HTTP body off of the link.
                    canRetry = AsyncHttpRetryHelper.IsTransientException(
                        exception, retryOptions, out statusCode, out webExceptionStatus, out socketError, out winHttpErrorCode, out curlErrorCode, includeIOException: false);

                    var requestDetail = ex.GetHttpMessageDetailsForTracing();
                    tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(SendAsync)}: {request.RequestUri} attempt {attempt}/{maxAttempts} failed with {nameof(HttpRequestException)}: '{ex.Message}', {nameof(HttpStatusCode)} {statusCode}, {nameof(WebExceptionStatus)} {webExceptionStatus}, {nameof(SocketError)} {socketError}, {nameof(WinHttpErrorCode)} {winHttpErrorCode}, {nameof(AsyncHttpRetryHelper.IsTransientException)} {canRetry}, {requestDetail}");
                }
                catch (IOException ex) when (ex.InnerException is SocketException)
                {
                    exception = ex.InnerException;
                    tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(SendAsync)}: {request.RequestUri} attempt {attempt}/{maxAttempts} failed with {nameof(SocketException)}");
                    if (canRetryOnSocketException)
                    {
                        canRetry = true;
                    }
                }
                catch (TimeoutException ex)
                {
                    exception = ex;
                    canRetry = true;

                    tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(SendAsync)}: {request.RequestUri} attempt {attempt}/{maxAttempts} failed with {nameof(TimeoutException)}: '{ex.Message}'");
                }

                if (attempt < maxAttempts && canRetry)
                {
                    attempt++;
                    backoff = GetExponentialBackoff(attempt, retryOptions.MinBackoff, retryOptions.MaxBackoff, retryOptions.BackoffCoefficient);

                    tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(SendAsync)}: {request.RequestUri} attempt {attempt}/{maxAttempts} will retry after {nameof(backoff)} {backoff}");

                    await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    canRetry = false;
                }
            }
            while (canRetry);

            if (exception != null)
            {
                exception.Data["AttemptCount"] = attempt;
                exception.Data["LastBackoff"] = backoff.Ticks / TimeSpan.TicksPerMillisecond;

                tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(SendAsync)}: {request.RequestUri} attempt {attempt}/{maxAttempts} throwing {exception.GetType().Name} with AttemptCount {attempt}, LastBackoff {exception.Data["LastBackoff"]}ms");

                throw exception;
            }

            return response;
        }

        protected override void TraceHttpRequestFailed(VssTraceActivity activity, HttpRequestMessage request, Exception exception)
        {
            base.TraceHttpRequestFailed(activity, request, exception);
            tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(TraceHttpRequestFailed)}: {request?.RequestUri}, exception {exception?.GetType().Name}, activity {activity?.Id}");
        }

        protected override void TraceHttpRequestFailed(VssTraceActivity activity, HttpRequestMessage request, HttpStatusCode statusCode, string afdRefInfo)
        {
            base.TraceHttpRequestFailed(activity, request, statusCode, afdRefInfo);
            tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(TraceHttpRequestFailed)}: {request?.RequestUri}, {nameof(HttpStatusCode)}.{statusCode}, activity {activity?.Id}");
        }

        protected override void TraceHttpRequestFailedMaxAttempts(VssTraceActivity activity, HttpRequestMessage request, int attempt, HttpStatusCode? httpStatusCode, WebExceptionStatus? webExceptionStatus, SocketError? socketErrorCode, WinHttpErrorCode? winHttpErrorCode, CurlErrorCode? curlErrorCode, string afdRefInfo)
        {
            base.TraceHttpRequestFailedMaxAttempts(activity, request, attempt, httpStatusCode, webExceptionStatus, socketErrorCode, winHttpErrorCode, curlErrorCode, afdRefInfo);
            tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(TraceHttpRequestFailedMaxAttempts)}: {request?.RequestUri}, attempt {attempt}, {nameof(HttpStatusCode)}.{httpStatusCode}, {nameof(WebExceptionStatus)}.{webExceptionStatus}, {nameof(SocketError)}.{socketErrorCode}, {nameof(WinHttpErrorCode)}.{winHttpErrorCode}, {nameof(CurlErrorCode)}.{curlErrorCode}, activity {activity?.Id}");
        }

        protected override void TraceHttpRequestRetrying(VssTraceActivity activity, HttpRequestMessage request, int attempt, TimeSpan backoffDuration, HttpStatusCode? httpStatusCode, WebExceptionStatus? webExceptionStatus, SocketError? socketErrorCode, WinHttpErrorCode? winHttpErrorCode, CurlErrorCode? curlErrorCode, string afdRefInfo)
        {
            base.TraceHttpRequestRetrying(activity, request, attempt, backoffDuration, httpStatusCode, webExceptionStatus, socketErrorCode, winHttpErrorCode, curlErrorCode, afdRefInfo);
            tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(TraceHttpRequestRetrying)}: {request?.RequestUri}, attempt {attempt}, backoff {backoffDuration}, {nameof(HttpStatusCode)}.{httpStatusCode}, {nameof(WebExceptionStatus)}.{webExceptionStatus}, {nameof(SocketError)}.{socketErrorCode}, {nameof(WinHttpErrorCode)}.{winHttpErrorCode}, {nameof(CurlErrorCode)}.{curlErrorCode}, activity {activity?.Id}");
        }

        protected override void TraceHttpRequestSucceededWithRetry(VssTraceActivity activity, HttpResponseMessage response, int attempt)
        {
            base.TraceHttpRequestSucceededWithRetry(activity, response, attempt);
            tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(TraceHttpRequestSucceededWithRetry)}: {nameof(HttpStatusCode)}.{response?.StatusCode}, attempt {attempt}, activity {activity?.Id}");
        }

        protected override void TraceRaw(HttpRequestMessage request, int tracepoint, TraceLevel level, string message, params object[] args)
        {
            base.TraceRaw(request, tracepoint, level, message, args);
            tracer.Verbose($"{nameof(ArtifactHttpRetryMessageHandler)}.{nameof(TraceRaw)}: {request?.RequestUri}, tracepoint {tracepoint}, {nameof(TraceLevel)}.{level}, message {SafeStringFormat.FormatSafe(message, args)}");
        }

        protected virtual async Task<HttpResponseMessage> GetResponseMessage(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Imported from ...\Microsoft.AspNet.WebApi.Client\lib\net45\System.Net.Http.Formatting.dll
        /// because it's not in ...\Microsoft.AspNet.WebApi.Client\lib\portable-wp8+netcore45+net45+wp81+wpa81\System.Net.Http.Formatting.dll
        /// </summary>
        private static HttpMessageHandler CreatePipeline(HttpMessageHandler innerHandler, IEnumerable<DelegatingHandler> handlers)
        {
            ArgumentUtility.CheckForNull(innerHandler, nameof(innerHandler));

            if (handlers == null)
            {
                return innerHandler;
            }

            HttpMessageHandler httpMessageHandler = innerHandler;
            IEnumerable<DelegatingHandler> enumerable = handlers.Reverse();
            foreach (DelegatingHandler item in enumerable)
            {
                if (item == null)
                {
                    throw new ArgumentException("System.Net.Http.Properties.Resources.DelegatingHandlerArrayContainsNullItem", nameof(handlers));
                }
                if (item.InnerHandler != null)
                {
                    throw new ArgumentException("System.Net.Http.Properties.Resources.DelegatingHandlerArrayHasNonNullInnerHandler", nameof(handlers));
                }

                item.InnerHandler = httpMessageHandler;
                httpMessageHandler = item;
            }
            return httpMessageHandler;
        }
    }
}
