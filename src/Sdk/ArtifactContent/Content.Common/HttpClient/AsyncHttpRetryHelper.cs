using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.WebApi;
using Microsoft.WindowsAzure.Storage;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// An asynchronous retry helper that returns <see cref="Task{T}"/>.
    /// This class is not thread-safe. There must be only a single active retry task for any instance of this class.
    /// </summary>
    public class AsyncHttpRetryHelper<TResult>
    {
        private static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DefaultDeltaBackoff = TimeSpan.FromSeconds(1);

        private readonly Func<Exception, Boolean> canRetryDelegate;
        private readonly IAppTraceSource tracer;
        private readonly Func<Task<TResult>> taskGenerator;
        private readonly int maxRetries;
        private readonly TimeSpan minBackoff;
        private readonly TimeSpan maxBackoff;
        private readonly TimeSpan deltaBackoff;
        private readonly Func<int, TimeSpan, TimeSpan, TimeSpan, TimeSpan> GetExponentialBackoff;
        private readonly bool continueOnCapturedContext;
        private readonly string context;

        protected static readonly List<int> RetryableHttpStatusCodes = new List<int>()
        {
            (int)HttpStatusCode.BadGateway,
            (int)HttpStatusCode.RequestTimeout,
            (int)VssNetworkHelper.TooManyRequests,
            (int)HttpStatusCode.InternalServerError,
            (int)HttpStatusCode.ServiceUnavailable,
            (int)HttpStatusCode.GatewayTimeout,
        };

        public AsyncHttpRetryHelper(
            Func<Task<TResult>> taskGenerator,
            Int32 maxRetries,
            IAppTraceSource tracer,
            bool continueOnCapturedContext,
            string context,
            Func<Exception, Boolean> canRetryDelegate = null,
            TimeSpan? minBackoff = null,
            TimeSpan? maxBackoff = null,
            TimeSpan? deltaBackoff = null,
            Func<int, TimeSpan, TimeSpan, TimeSpan, TimeSpan> getExponentialBackoff = null)
        {
            ArgumentUtility.CheckForNull(tracer, nameof(tracer));
            this.tracer = tracer;
            this.continueOnCapturedContext = continueOnCapturedContext;
            this.context = $"[{context}] ";

            this.maxRetries = maxRetries;
            this.taskGenerator = taskGenerator;
            this.canRetryDelegate = canRetryDelegate;
            this.minBackoff = minBackoff ?? DefaultMinBackoff;
            this.maxBackoff = maxBackoff ?? DefaultMaxBackoff;
            this.deltaBackoff = deltaBackoff ?? DefaultDeltaBackoff;
            this.GetExponentialBackoff = getExponentialBackoff ?? BackoffTimerHelper.GetExponentialBackoff;
        }

        // This mutable instance variable prevents this class from being thread-safe.
        //
        public int RemainingRetries { get; private set; }

        /// <summary>
        /// Execute a Task with retries.
        /// </summary>
        /// <param name="taskGenerator">a task generator that can create the same task on retry</param>
        /// <param name="maxRetries">the maximum of retries</param>
        /// <param name="tracer">a tracer to log the retries</param>
        /// <param name="canRetryDelegate">an optional delegate that can be used to determine if a retry should be performed, in case the default heuristics returns false</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort the operation by the caller</param>
        public static Task<TResult> InvokeAsync(
            Func<Task<TResult>> taskGenerator,
            int maxRetries,
            IAppTraceSource tracer,
            Func<Exception, bool> canRetryDelegate,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext,
            string context,
            TimeSpan? minBackoff = null,
            TimeSpan? maxBackoff = null,
            TimeSpan? deltaBackoff = null)
        {
            var retryHelper = new AsyncHttpRetryHelper<TResult>(taskGenerator, maxRetries, tracer, continueOnCapturedContext, context, canRetryDelegate, minBackoff, maxBackoff, deltaBackoff);
            return retryHelper.InvokeAsync(cancellationToken);
        }

        /// <summary>
        /// Execute a Task with retries.
        /// This method is not thread-safe. There must be only a single active retry task for any instance of this class.        
        /// </summary>
        public async Task<TResult> InvokeAsync(CancellationToken cancellationToken)
        {
            int remainingRetries = this.maxRetries;
            this.RemainingRetries = remainingRetries;
            TimeSpan? lastBackoff = null;
            TimeSpan timeToDelay = TimeSpan.MinValue;

            try
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        return await taskGenerator().ConfigureAwait(continueOnCapturedContext);
                    }
                    catch (Exception exception)
                    {
                        int zeroBasedAttemptCount = maxRetries - remainingRetries;
                        int oneBasedAttemptCount = 1 + zeroBasedAttemptCount;
                        bool retryable = IsRetryable(exception, this.canRetryDelegate, cancellationToken);
                        string responseDetails = exception.GetHttpMessageDetailsForTracing();

                        if (!retryable)
                        {
                            // Case 1 - Not retryable - throw
                            // If cancellation is requested, do not try to retrieve HTTP message details.
                            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                            {
                                responseDetails = "Task was requested to be canceled."; 
                            }
                            this.tracer.Warn(exception, $"{context}Try {oneBasedAttemptCount}/{maxRetries}, non-retryable exception caught. Throwing. Details:\r\n{responseDetails}");
                            exception.Data["AttemptCount"] = oneBasedAttemptCount;
                            exception.Data["LastBackoff"] = lastBackoff;
                            throw;
                        }
                        else
                        {
                            if (remainingRetries <= 0)
                            {
                                // Case 2 - Retryable, but no more retries - throw
                                this.tracer.Warn(exception, $"{context}Try {oneBasedAttemptCount}/{maxRetries}, retryable exception caught, but retries have been exhausted. Details:\r\n{responseDetails}");
                                exception.Data["AttemptCount"] = oneBasedAttemptCount;
                                exception.Data["LastBackoff"] = lastBackoff;
                                throw;
                            }
                            else
                            {
                                // Case 3 - Retryable and has more retries - retry
                                timeToDelay = GetExponentialBackoff(zeroBasedAttemptCount, minBackoff, maxBackoff, deltaBackoff);
                                this.tracer.Warn(exception, $"{context}Try {oneBasedAttemptCount}/{maxRetries}, retryable exception caught. Retrying in {timeToDelay}. Details:\r\n{responseDetails}");
                            }

                            remainingRetries--;
                        }
                    }

                    // Delay before we try again
                    await Task.Delay(timeToDelay, cancellationToken).ConfigureAwait(this.continueOnCapturedContext);
                    lastBackoff = timeToDelay;
                }
                while (remainingRetries >= 0);
            }
            finally
            {
                this.RemainingRetries = remainingRetries;
            }

            throw new ConstraintException($"{context}This exception should not be reachable.");
        }
        
        /// <summary>
        /// Return true if the exception is considered to be retryable.
        /// </summary>
        private bool IsRetryable(Exception exception, Func<Exception, bool> canRetryDelegate, CancellationToken cancellationToken)
        {
            return
                ((AsyncHttpRetryHelper.IsTransientException(exception) || // Transient network error
                (canRetryDelegate != null && canRetryDelegate(exception))) || // Meet customized criteria
                ((exception is OperationCanceledException) && !cancellationToken.IsCancellationRequested)); // Canceled but not from the token
        }
    }

    /// <summary>
    /// An asynchronous retry helper that returns <see cref="Task"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AsyncHttpRetryHelper : AsyncHttpRetryHelper<int>
    {
        [Serializable]
        public class RetryableException : Exception
        {
            public RetryableException(String message)
            : base(message)
            {
            }

            public RetryableException(String message, Exception ex)
            : base(message, ex)
            {
            }

            public RetryableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            {
            }
        }

        /// <summary>
        /// Execute a Task with retries.
        /// </summary>
        /// <remarks>
        /// This method will retry on 
        /// <see cref="HttpRequestException"/> (if the status code indicates of a transient error or the inner exception is an <see cref="System.IO.IOException"/>), 
        /// <see cref="TimeoutException"/>, 
        /// <see cref="VssServiceException"/>, 
        /// and any exceptions considered to be transient by <see cref="VssNetworkHelper.IsTransientNetworkException(Exception)"/>.
        /// </remarks>
        /// <param name="taskGenerator">a task generator that can create the same task on retry</param>
        /// <param name="maxRetries">the maximum of retries</param>
        /// <param name="tracer">a tracer to log the retries</param>
        public static Task<TResult> InvokeAsync<TResult>(
            Func<Task<TResult>> taskGenerator,
            int maxRetries,
            IAppTraceSource tracer,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext,
            string context = null)
        {
            return InvokeAsync(taskGenerator, maxRetries, tracer, canRetryDelegate: null, cancellationToken: cancellationToken, continueOnCapturedContext: continueOnCapturedContext, context);
        }
        
        /// <summary>
        /// Execute a Task with retries.
        /// </summary>
        /// <param name="taskGenerator">a task generator that can create the same task on retry</param>
        /// <param name="maxRetries">the maximum of retries</param>
        /// <param name="tracer">a tracer to log the retries</param>
        /// <param name="canRetryDelegate">an optional delegate that can be used to determine if a retry should be performed, in case the default heuristics returns false</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort the operation by the caller</param>
        public static Task<TResult> InvokeAsync<TResult>(
            Func<Task<TResult>> taskGenerator,
            int maxRetries,
            IAppTraceSource tracer,
            Func<Exception, bool> canRetryDelegate,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext,
            string context = null)
        {
            var retryHelper = new AsyncHttpRetryHelper<TResult>(
                taskGenerator,
                maxRetries,
                tracer,
                continueOnCapturedContext,
                context,
                canRetryDelegate);

            return retryHelper.InvokeAsync(cancellationToken);
        }

        public static Task InvokeVoidAsync(Func<Task> taskGenerator, int maxRetries, IAppTraceSource tracer, CancellationToken cancellationToken, bool continueOnCapturedContext, string context)
        {
            return InvokeVoidAsync(taskGenerator, maxRetries, tracer, canRetryDelegate: null, cancellationToken, continueOnCapturedContext, context);
        }

        public AsyncHttpRetryHelper(Func<Task> taskGenerator, Int32 maxRetries, IAppTraceSource tracer, bool continueOnCapturedContext, string context, Func<Exception, Boolean> canRetryDelegate = null)
            : base(CreateTaskWrapper(taskGenerator, continueOnCapturedContext), maxRetries, tracer, continueOnCapturedContext, context, canRetryDelegate)
        {
        }

        /// <summary>
        /// Execute a Task with retries.
        /// </summary>
        public static Task InvokeVoidAsync(Func<Task> taskGenerator, int maxRetries, IAppTraceSource tracer, Func<Exception, bool> canRetryDelegate, CancellationToken cancellationToken, bool continueOnCapturedContext, string context)
        {
            return AsyncHttpRetryHelper<int>.InvokeAsync(CreateTaskWrapper(taskGenerator, continueOnCapturedContext), maxRetries, tracer, canRetryDelegate, cancellationToken, continueOnCapturedContext, context);
        }

        /// <summary>
        /// Check if an exception is transient. An <see cref="HttpRequestException"/> caused by <see cref="IOException"/> is considered transient too.
        /// </summary>
        public static bool IsTransientException(
            Exception exception)
        {
            if (exception is RetryableException)
            {
                return true;
            }

            if (exception is StorageException && exception.InnerException is TimeoutException)
            {
                return true;
            }

            HttpStatusCode? httpStatusCode;
            WebExceptionStatus? webExceptionStatus;
            SocketError? socketErrorCode;
            WinHttpErrorCode? winHttpErrorCode;
            CurlErrorCode? curlErrorCode;
            return IsTransientException(
                exception, new VssHttpRetryOptions(), out httpStatusCode, out webExceptionStatus, out socketErrorCode, out winHttpErrorCode, out curlErrorCode, includeIOException: true);
        }

        /// <summary>
        /// An augmented version of transient exception heuristic analyzer.
        /// </summary>
        /// <remarks>
        /// Thie method will also consider the following exceptions as transient issues: 
        /// <see cref="System.Net.Http.HttpRequestException"/> where the HTTP code is 408, 502, 503 or 504; or where the inner exception is IOException,
        /// <see cref="System.TimeoutException"/>,
        /// <see cref="GitHub.Services.Common.VssServiceException"/>,
        /// <see cref="System.IO.IOException"/> (only if <paramref name="includeIOException"/> is true. Note Retrying on HTTP client handler with this
        /// kind of exception may not work since the IOException is thrown during the body tranmission while the handler has already returned once the 
        /// headers are generated.)
        /// </remarks>
        internal static bool IsTransientException(
            Exception exception,
            VssHttpRetryOptions retryOptions,
            out HttpStatusCode? statusCode,
            out WebExceptionStatus? webExceptionStatus,
            out SocketError? socketError,
            out WinHttpErrorCode? winHttpErrorCode,
            out CurlErrorCode? curlErrorCode,
            bool includeIOException)
        {
            if (retryOptions == null)
            {
                retryOptions = new VssHttpRetryOptions();
            }

            bool isTransient = VssNetworkHelper.IsTransientNetworkException(exception, retryOptions, out statusCode, out webExceptionStatus, out socketError, out winHttpErrorCode, out curlErrorCode);

            // Examples that are not retried
            // 403 "Time-Limited URL validation failed" -- error when a SAS URL expires

            // When reading a content stream HttpClient will return exceptions as HttpRequestException, not WebException.
            // Had they been WebException, VssHttpRetryOptions would have considered the following to be retryable status codes.
            isTransient |= exception is HttpRequestException &&
                          (exception.Message.Contains("408") ||  // Request Timeout
                           exception.Message.Contains("429") ||  // Too Many Requests
                           exception.Message.Contains("500") ||  // Internal Server Error
                           exception.Message.Contains("502") ||  // Bad Gateway
                           exception.Message.Contains("503") ||  // Service Unavailable
                           exception.Message.Contains("504") ||  // Gateway Timeout
                           exception.InnerException is WebException || // The request was aborted: The request was canceled.
                           (includeIOException && exception.InnerException is IOException)); // Error while copying content to a stream

            isTransient |= exception is TimeoutException;
            // DEVNOTE: Commenting out the VssServiceException because it is too generic to
            // determine the exception's transiency on.
            // At the moment, any user defined exception that fails type-translation becomes
            // a VssServiceException and the client retries, excessively.
            //isTransient |= exception is VssServiceException;
            isTransient |= IsServiceRequestExceptionTransient(exception);
            isTransient |= exception is IOException;
            isTransient |= exception is StorageException &&
                          (exception.Message.Contains("500") ||
                           exception.Message.Contains("503"));

            return isTransient;
        }

        /// <summary>
        /// IsServiceRequestExceptionTransient - VssServiceResponseException's are the fallback
        /// when the thrown exception is an InvalidOperationException or other generic .NET types.
        /// In such cases, it isn't prudent to retry blindly and hence the filter below ensures that if
        /// the exception is indeed of the type 'VssServiceResponseException' then the retries happen only
        /// if the HTTPStatusCode is amongst the retryable ones.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>True, if transient. False otherwise.</returns>
        private static bool IsServiceRequestExceptionTransient(Exception exception)
        {
            if (exception is VssServiceResponseException serviceResponseException)
            {
                return RetryableHttpStatusCodes.Contains((int)serviceResponseException.HttpStatusCode);
            }

            // ExceptionType isn't VssServiceResponseException nor derived from it
            // so it doesn't include the HttpStatusCode so it can't be checked for
            // transience based on its code.
            return false;
        }

        private static Func<Task<int>> CreateTaskWrapper(Func<Task> taskGenerator, bool continueOnCapturedContext)
        {
            Func<Task<int>> wrapper = async () =>
            {
                await taskGenerator().ConfigureAwait(continueOnCapturedContext);
                return 0;
            };

            return wrapper;
        }
    }
}
