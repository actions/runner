using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace GitHub.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class VssNetworkHelper
    {
        /// <summary>
        /// Heuristic used to determine whether an exception is a transient network
        /// failure that should be retried.
        /// </summary>
        public static bool IsTransientNetworkException(Exception ex)
        {
            return IsTransientNetworkException(ex, new VssHttpRetryOptions());
        }

        /// <summary>
        /// Heuristic used to determine whether an exception is a transient network
        /// failure that should be retried.
        /// </summary>
        public static bool IsTransientNetworkException(
            Exception ex,
            VssHttpRetryOptions options)
        {
            HttpStatusCode? httpStatusCode;
            WebExceptionStatus? webExceptionStatus;
            SocketError? socketErrorCode;
            WinHttpErrorCode? winHttpErrorCode;
            CurlErrorCode? curlErrorCode;
            return IsTransientNetworkException(ex, options, out httpStatusCode, out webExceptionStatus, out socketErrorCode, out winHttpErrorCode, out curlErrorCode);
        }

        /// <summary>
        /// Heuristic used to determine whether an exception is a transient network
        /// failure that should be retried.
        /// </summary>
        public static bool IsTransientNetworkException(
            Exception ex,
            out HttpStatusCode? httpStatusCode,
            out WebExceptionStatus? webExceptionStatus,
            out SocketError? socketErrorCode,
            out WinHttpErrorCode? winHttpErrorCode,
            out CurlErrorCode? curlErrorCode)
        {
            return IsTransientNetworkException(ex, VssHttpRetryOptions.Default, out httpStatusCode, out webExceptionStatus, out socketErrorCode, out winHttpErrorCode, out curlErrorCode);
        }

        /// <summary>
        /// Heuristic used to determine whether an exception is a transient network
        /// failure that should be retried.
        /// </summary>
        public static bool IsTransientNetworkException(
            Exception ex,
            VssHttpRetryOptions options,
            out HttpStatusCode? httpStatusCode,
            out WebExceptionStatus? webExceptionStatus,
            out SocketError? socketErrorCode,
            out WinHttpErrorCode? winHttpErrorCode,
            out CurlErrorCode? curlErrorCode)
        {
            httpStatusCode = null;
            webExceptionStatus = null;
            socketErrorCode = null;
            winHttpErrorCode = null;
            curlErrorCode = null;

            while (ex != null)
            {
                if (IsTransientNetworkExceptionHelper(ex, options, out httpStatusCode, out webExceptionStatus, out socketErrorCode, out winHttpErrorCode, out curlErrorCode))
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        /// <summary>
        /// Helper which checks a particular Exception instance (non-recursive).
        /// </summary>
        private static bool IsTransientNetworkExceptionHelper(
            Exception ex,
            VssHttpRetryOptions options,
            out HttpStatusCode? httpStatusCode,
            out WebExceptionStatus? webExceptionStatus,
            out SocketError? socketErrorCode,
            out WinHttpErrorCode? winHttpErrorCode,
            out CurlErrorCode? curlErrorCode)
        {
            ArgumentUtility.CheckForNull(ex, "ex");

            httpStatusCode = null;
            webExceptionStatus = null;
            socketErrorCode = null;
            winHttpErrorCode = null;
            curlErrorCode = null;

            if (ex is WebException)
            {
                WebException webEx = (WebException)ex;

                if (webEx.Response != null && webEx.Response is HttpWebResponse)
                {
                    var httpResponse = (HttpWebResponse)webEx.Response;
                    httpStatusCode = httpResponse.StatusCode;

                    // If the options include this status code as a retryable error then we report the exception
                    // as transient to the caller
                    if (options.RetryableStatusCodes.Contains(httpResponse.StatusCode))
                    {
                        return true;
                    }
                }

                webExceptionStatus = webEx.Status;

                if (webEx.Status == WebExceptionStatus.ConnectFailure ||
                    webEx.Status == WebExceptionStatus.ConnectionClosed ||
                    webEx.Status == WebExceptionStatus.KeepAliveFailure ||
                    webEx.Status == WebExceptionStatus.NameResolutionFailure ||
                    webEx.Status == WebExceptionStatus.ReceiveFailure ||
                    webEx.Status == WebExceptionStatus.SendFailure ||
                    webEx.Status == WebExceptionStatus.Timeout)
                {
                    return true;
                }
            }
            else if (ex is SocketException)
            {
                SocketException sockEx = (SocketException)ex;

                socketErrorCode = sockEx.SocketErrorCode;

                if (sockEx.SocketErrorCode == SocketError.Interrupted ||
                    sockEx.SocketErrorCode == SocketError.NetworkDown ||
                    sockEx.SocketErrorCode == SocketError.NetworkUnreachable ||
                    sockEx.SocketErrorCode == SocketError.NetworkReset ||
                    sockEx.SocketErrorCode == SocketError.ConnectionAborted ||
                    sockEx.SocketErrorCode == SocketError.ConnectionReset ||
                    sockEx.SocketErrorCode == SocketError.TimedOut ||
                    sockEx.SocketErrorCode == SocketError.HostDown ||
                    sockEx.SocketErrorCode == SocketError.HostUnreachable ||
                    sockEx.SocketErrorCode == SocketError.TryAgain)
                {
                    return true;
                }
            }
            else if (ex is Win32Exception) // WinHttpException when use WinHttp (dotnet core)
            {
                Win32Exception winHttpEx = (Win32Exception)ex;

                Int32 errorCode = winHttpEx.NativeErrorCode;
                if (errorCode > (Int32)WinHttpErrorCode.WINHTTP_ERROR_BASE &&
                    errorCode <= (Int32)WinHttpErrorCode.WINHTTP_ERROR_LAST)
                {
                    winHttpErrorCode = (WinHttpErrorCode)errorCode;

                    if (winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_CANNOT_CONNECT ||
                        winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_CONNECTION_ERROR ||
                        winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_INTERNAL_ERROR ||
                        winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_NAME_NOT_RESOLVED ||
                        winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_TIMEOUT)
                    {
                        return true;
                    }
                }
            }
            else if (ex is IOException)
            {
                if (null != ex.InnerException &&
                    ex.InnerException is Win32Exception)
                {
                    String stackTrace = ex.StackTrace;

                    if (null != stackTrace &&
                        stackTrace.IndexOf("System.Net.Security._SslStream.StartWriting(", StringComparison.Ordinal) >= 0)
                    {
                        // HACK: There is an underlying HRESULT code for this error which is not set on the exception which 
                        //       bubbles from the underlying stack. The top of the stack trace will be in the _SslStream class
                        //       and will have an exception chain of HttpRequestException -> IOException -> Win32Exception.

                        // Check for SEC_E_CONTEXT_EXPIRED as this occurs at random in the underlying stack. Retrying the
                        // request should get a new connection and work correctly, so we ignore this particular error.

                        return true;
                    }
                }
            }
            else if (ex.GetType().Name == "CurlException") // CurlException when use libcurl (dotnet core)
            {
                // Valid curl error code should in range (0, 93]
                if (ex.HResult > 0 && ex.HResult < 94)
                {
                    curlErrorCode = (CurlErrorCode)ex.HResult;
                    if (curlErrorCode == CurlErrorCode.CURLE_COULDNT_RESOLVE_PROXY ||
                        curlErrorCode == CurlErrorCode.CURLE_COULDNT_RESOLVE_HOST ||
                        curlErrorCode == CurlErrorCode.CURLE_COULDNT_CONNECT ||
                        curlErrorCode == CurlErrorCode.CURLE_HTTP2 ||
                        curlErrorCode == CurlErrorCode.CURLE_PARTIAL_FILE ||
                        curlErrorCode == CurlErrorCode.CURLE_WRITE_ERROR ||
                        curlErrorCode == CurlErrorCode.CURLE_UPLOAD_FAILED ||
                        curlErrorCode == CurlErrorCode.CURLE_READ_ERROR ||
                        curlErrorCode == CurlErrorCode.CURLE_OPERATION_TIMEDOUT ||
                        curlErrorCode == CurlErrorCode.CURLE_INTERFACE_FAILED ||
                        curlErrorCode == CurlErrorCode.CURLE_GOT_NOTHING ||
                        curlErrorCode == CurlErrorCode.CURLE_SEND_ERROR ||
                        curlErrorCode == CurlErrorCode.CURLE_RECV_ERROR)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the HttpStatusCode which represents a throttling error.
        /// </summary>
        public const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
    }
}
