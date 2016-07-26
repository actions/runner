using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    /// <summary>
    /// Re-attempt non-state changing requests on timeout, which VssHttpRetryMessageHandler doesn't consider retryable.
    /// </summary>
    public class HttpRetryOnTimeoutMessageHandler : DelegatingHandler
    {
        private readonly HttpRetryOnTimeoutOptions _retryOptions;
        private readonly IConatinerFetchEngineLogger _logger;

        public HttpRetryOnTimeoutMessageHandler(HttpRetryOnTimeoutOptions retryOptions, IConatinerFetchEngineLogger logger)
        {
            _retryOptions = retryOptions;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Potential improvements:
            // 1. Calculate a max time across attempts, to avoid retries (this class) on top of retries (VssHttpRetryMessageHandler) 
            //    causing more time to pass than expected in degenerative cases.
            // 2. Increase the per-attempt timeout on each attempt. Instead of 5 minutes on each attempt, start low and build to 10-20 minutes.

            HttpResponseMessage response = null;

            // We can safely retry on timeout if the request isn't one that changes state.
            Boolean canRetry = (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head || request.Method == HttpMethod.Options);

            if (canRetry)
            {
                Int32 attempt = 1;
                TimeoutException exception = null;
                Int32 maxAttempts = _retryOptions.MaxRetries + 1;

                while (attempt <= maxAttempts)
                {
                    // Reset the exception so we don't have a lingering variable
                    exception = null;

                    Stopwatch watch = Stopwatch.StartNew();
                    try
                    {
                        response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                        break;
                    }
                    catch (TimeoutException ex)
                    {
                        exception = ex;
                    }

                    TimeSpan backoff;
                    if (attempt < maxAttempts)
                    {
                        backoff = BackoffTimerHelper.GetExponentialBackoff(
                            attempt,
                            _retryOptions.MinBackoff,
                            _retryOptions.MaxBackoff,
                            _retryOptions.BackoffCoefficient);
                    }
                    else
                    {
                        break;
                    }

                    string message = StringUtil.Loc("RMContainerItemRequestTimedOut", (int) watch.Elapsed.TotalSeconds, backoff.TotalSeconds, request.Method, request.RequestUri);
                    _logger.Warning(message);

                    attempt++;
                    await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                }

                if (exception != null)
                {
                    throw exception;
                }
            }
            else
            {
                // No retries. Just pipe the request through to the other handlers.
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
    }
}
