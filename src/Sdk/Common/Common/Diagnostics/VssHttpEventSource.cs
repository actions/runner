using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace GitHub.Services.Common.Diagnostics
{
    [EventSource(Name = VssEventSources.Http)]
    internal sealed class VssHttpEventSource : EventSource
    {
        public static class Tasks
        {
            public const EventTask HttpRequest = (EventTask)1;
            public const EventTask Authentication = (EventTask)2;
            public const EventTask HttpOperation = (EventTask)3;
        }

        public static class Keywords
        {
            public const EventKeywords Authentication = (EventKeywords)0x0000000000000001;
            public const EventKeywords HttpOperation = (EventKeywords)0x0000000000000002;
        }

        /// <summary>
        /// Gets the singleton event source used for logging.
        /// </summary>
        internal static VssHttpEventSource Log
        {
            get
            {
                return m_log.Value;
            }
        }

        [NonEvent]
        public void AuthenticationStart(VssTraceActivity activity)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                AuthenticationStart();
            }
        }

        [NonEvent]
        public void AuthenticationStop(VssTraceActivity activity)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                AuthenticationStop();
            }
        }

        [NonEvent]
        public void AuthenticationError(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            String message)
        {
            if (IsEnabled(EventLevel.Error, Keywords.Authentication))
            {
                SetActivityId(activity);
                WriteMessageEvent(provider.CredentialType, provider.GetHashCode(), message, this.AuthenticationError);
            }
        }

        [NonEvent]
        public void AuthenticationError(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            Exception exception)
        {
            if (IsEnabled(EventLevel.Error, Keywords.Authentication))
            {
                if (exception is AggregateException)
                {
                    exception = ((AggregateException)exception).Flatten().InnerException;
                }

                AuthenticationError(activity, provider, exception.ToString());
            }
        }

        [NonEvent]
        public void HttpOperationStart(
            VssTraceActivity activity,
            String area,
            String operation)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpOperationStart(area, operation);
            }
        }

        [NonEvent]
        public void HttpOperationStop(
            VssTraceActivity activity,
            String area,
            String operation)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpOperationStop(area, operation);
            }
        }

        [NonEvent]
        public void HttpRequestStart(
            VssTraceActivity activity,
            HttpRequestMessage request)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestStart(request.GetHttpMethod(), request.RequestUri.AbsoluteUri);
            }
        }

        [NonEvent]
        public Exception HttpRequestFailed(
            VssTraceActivity activity,
            HttpRequestMessage request,
            Exception exception)
        {
            if (IsEnabled())
            {
                HttpRequestFailed(activity, request, exception.ToString());
            }
            return exception;
        }

        [NonEvent]
        public void HttpRequestFailed(
            VssTraceActivity activity,
            HttpRequestMessage request,
            String message)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                WriteMessageEvent(request.GetHttpMethod(), request.RequestUri.AbsoluteUri, message, this.HttpRequestFailed);
            }
        }

        [NonEvent]
        public void HttpRequestFailed(
            VssTraceActivity activity,
            HttpRequestMessage request,
            HttpStatusCode statusCode,
            string afdRefInfo)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                CultureInfo cultureInfo = CultureInfo.InstalledUICulture;
                String message = String.Format(cultureInfo, "HTTP Status: {0}", statusCode);

                if (!string.IsNullOrEmpty(afdRefInfo))
                {
                    message += $", AFD Ref: {afdRefInfo}";
                }

                WriteMessageEvent(request.GetHttpMethod(), request.RequestUri.AbsoluteUri, message, this.HttpRequestFailed);
            }
        }

        [NonEvent]
        public void HttpRequestUnauthorized(
            VssTraceActivity activity,
            HttpRequestMessage request,
            String message)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestUnauthorized(request.GetHttpMethod(), request.RequestUri.AbsoluteUri, message);
            }
        }

        [NonEvent]
        public void HttpRequestSucceeded(
            VssTraceActivity activity,
            HttpResponseMessage response)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestSucceeded(response.RequestMessage.GetHttpMethod(), response.RequestMessage.RequestUri.AbsoluteUri, (Int32)response.StatusCode);
            }
        }

        [NonEvent]
        public void HttpRequestRetrying(
            VssTraceActivity activity,
            HttpRequestMessage request,
            Int32 attempt,
            TimeSpan backoffDuration,
            HttpStatusCode? httpStatusCode,
            WebExceptionStatus? webExceptionStatus,
            SocketError? socketErrorCode,
            WinHttpErrorCode? winHttpErrorCode,
            CurlErrorCode? curlErrorCode,
            string afdRefInfo)
        {
            if (IsEnabled())
            {
                String reason = "<unknown>";
                if (httpStatusCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "HTTP Status: {0}", httpStatusCode.Value);
                }
                else if (webExceptionStatus != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "Web Exception Status: {0}", webExceptionStatus.Value);
                }
                else if (socketErrorCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "Socket Error: {0}", socketErrorCode.Value);
                }
                else if (winHttpErrorCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "WinHttp Error: {0}", winHttpErrorCode);
                }
                else if (curlErrorCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "Curl Error: {0}", curlErrorCode);
                }

                if (!string.IsNullOrEmpty(afdRefInfo))
                {
                    reason += $", AFD Ref: {afdRefInfo}";
                }

                SetActivityId(activity);
                HttpRequestRetrying(request.GetHttpMethod(), request.RequestUri.AbsoluteUri, attempt, reason, backoffDuration.TotalSeconds);
            }
        }

        [NonEvent]
        public void HttpRequestFailedMaxAttempts(
            VssTraceActivity activity,
            HttpRequestMessage request,
            Int32 attempt,
            HttpStatusCode? httpStatusCode,
            WebExceptionStatus? webExceptionStatus,
            SocketError? socketErrorCode,
            WinHttpErrorCode? winHttpErrorCode,
            CurlErrorCode? curlErrorCode,
            string afdRefInfo)
        {
            if (IsEnabled())
            {
                String reason = "<unknown>";
                if (httpStatusCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "HTTP Status: {0}", httpStatusCode.Value);
                }
                else if (webExceptionStatus != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "Web Exception Status: {0}", webExceptionStatus.Value);
                }
                else if (socketErrorCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "Socket Error: {0}", socketErrorCode.Value);
                }
                else if (winHttpErrorCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "WinHttp Error: {0}", winHttpErrorCode);
                }
                else if (curlErrorCode != null)
                {
                    reason = String.Format(CultureInfo.InvariantCulture, "Curl Error: {0}", curlErrorCode);
                }

                if (!string.IsNullOrEmpty(afdRefInfo))
                {
                    reason += $", AFD Ref: {afdRefInfo}";
                }

                SetActivityId(activity);
                HttpRequestFailedMaxAttempts(request.GetHttpMethod(), request.RequestUri.AbsoluteUri, attempt, reason);
            }
        }

        [NonEvent]
        public void HttpRequestSucceededWithRetry(
            VssTraceActivity activity,
            HttpResponseMessage response,
            Int32 attempt)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestSucceededWithRetry(response.RequestMessage.GetHttpMethod(), response.RequestMessage.RequestUri.AbsoluteUri, attempt);
            }
        }

        [NonEvent]
        public void HttpRequestCancelled(
            VssTraceActivity activity,
            HttpRequestMessage request)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestCancelled(request.GetHttpMethod(), request.RequestUri.AbsoluteUri);
            }
        }

        [NonEvent]
        public void HttpRequestTimedOut(
            VssTraceActivity activity,
            HttpRequestMessage request,
            TimeSpan timeout)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestTimedOut(request.GetHttpMethod(), request.RequestUri.AbsoluteUri, (Int32)timeout.TotalSeconds);
            }
        }

        [NonEvent]
        public void HttpRequestStop(
            VssTraceActivity activity,
            HttpResponseMessage response)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                HttpRequestStop(response.RequestMessage.GetHttpMethod(), response.RequestMessage.RequestUri.AbsoluteUri, (Int32)response.StatusCode);
            }
        }

        [NonEvent]
        public void AuthenticationFailed(
            VssTraceActivity activity,
            HttpResponseMessage response)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                WriteMessageEvent((Int32)response.StatusCode, response.Headers.ToString(), this.AuthenticationFailed);
            }
        }

        [NonEvent]
        public void IssuedTokenProviderCreated(
            VssTraceActivity activity,
            IssuedTokenProvider provider)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenProviderCreated(provider.CredentialType, provider.GetHashCode(), provider.GetAuthenticationParameters());
            }
        }

        [NonEvent]
        public void IssuedTokenProviderRemoved(
            VssTraceActivity activity,
            IssuedTokenProvider provider)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenProviderRemoved(provider.CredentialType, provider.GetHashCode(), provider.GetAuthenticationParameters());
            }
        }

        [NonEvent]
        internal void IssuedTokenProviderNotFound(VssTraceActivity activity)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenProviderNotFound();
            }
        }

        [NonEvent]
        internal void IssuedTokenProviderPromptRequired(
            VssTraceActivity activity,
            IssuedTokenProvider provider)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenProviderPromptRequired(provider.CredentialType, provider.GetHashCode());
            }
        }

        [NonEvent]
        public void IssuedTokenAcquiring(
            VssTraceActivity activity,
            IssuedTokenProvider provider)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenAcquiring(provider.CredentialType, provider.GetHashCode());
            }
        }

        [NonEvent]
        public void IssuedTokenWaitStart(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            Guid waitForActivityId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                SetActivityId(activity);
                IssuedTokenWaitStart(provider.CredentialType, provider.GetHashCode(), waitForActivityId);
            }
        }

        [NonEvent]
        public void IssuedTokenWaitStop(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            IssuedToken token)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                SetActivityId(activity);
                IssuedTokenWaitStop(provider.CredentialType, provider.GetHashCode(), token != null ? token.GetHashCode() : 0);
            }
        }

        [NonEvent]
        public void IssuedTokenAcquired(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            IssuedToken token)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenAcquired(provider.CredentialType, provider.GetHashCode(), token != null ? token.GetHashCode() : 0);
            }
        }

        [NonEvent]
        public void IssuedTokenInvalidated(
            VssTraceActivity activity,
            IssuedTokenProvider provider, 
            IssuedToken token)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenInvalidated(provider.CredentialType, provider.GetHashCode(), token.GetHashCode());
            }
        }

        [NonEvent]
        public void IssuedTokenValidated(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            IssuedToken token)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenValidated(provider.CredentialType, provider.GetHashCode(), token.GetHashCode());
            }
        }

        [NonEvent]
        public void IssuedTokenRetrievedFromCache(
            VssTraceActivity activity,
            IssuedTokenProvider provider,
            IssuedToken token)
        {
            if (IsEnabled())
            {
                SetActivityId(activity);
                IssuedTokenRetrievedFromCache(provider.CredentialType, provider.GetHashCode(), token.GetHashCode());
            }
        }

        [Event(1, Level = EventLevel.Verbose, Task = Tasks.HttpRequest, Opcode = EventOpcode.Start, Message = "Started {0} request to {1}")]
        private void HttpRequestStart(
            VssHttpMethod method,
            String url)
        {
            if (IsEnabled())
            {
                WriteEvent(1, (Int32)method, url);
            }
        }

        [Event(2, Level = EventLevel.Error, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "{0} request to {1} failed. {2}")]
        private void HttpRequestFailed(
            VssHttpMethod method,
            String url,
            String message)
        {
            if (IsEnabled())
            {
                WriteEvent(2, (Int32)method, url, message);
            }
        }

        [Event(3, Level = EventLevel.Informational, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "{0} request to {1} succeeded with status code {2}")]
        private void HttpRequestSucceeded(
            VssHttpMethod method,
            String url,
            Int32 statusCode)
        {
            if (IsEnabled())
            {
                WriteEvent(3, (Int32)method, url, statusCode);
            }
        }

        [Event(4, Level = EventLevel.Warning, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "Attempt {2} of {0} request to {1} failed ({3}). The operation will be retried in {4} seconds.")]
        private void HttpRequestRetrying(
            VssHttpMethod method,
            String url,
            Int32 attempt,
            String reason,
            Double backoffDurationInSeconds)
        {
            if (IsEnabled())
            {
                WriteEvent(4, (Int32)method, url, attempt, reason, backoffDurationInSeconds);
            }
        }

        [Event(5, Level = EventLevel.Error, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "Attempt {2} of {0} request to {1} failed ({3}). The maximum number of attempts has been reached.")]
        private void HttpRequestFailedMaxAttempts(
            VssHttpMethod method,
            String url,
            Int32 attempt,
            String reason)
        {
            if (IsEnabled())
            {
                WriteEvent(5, (Int32)method, url, attempt, reason);
            }
        }

        [Event(6, Level = EventLevel.Verbose, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "Attempt {2} of {0} request to {1} succeeded.")]
        private void HttpRequestSucceededWithRetry(
            VssHttpMethod method,
            String url,
            Int32 attempt)
        {
            if (IsEnabled())
            {
                WriteEvent(6, (Int32)method, url, attempt);
            }
        }

        [Event(7, Level = EventLevel.Warning, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "{0} request to {1} has been cancelled.")]
        private void HttpRequestCancelled(
            VssHttpMethod method,
            String url)
        {
            if (IsEnabled())
            {
                WriteEvent(7, (Int32)method, url);
            }
        }

        [Event(8, Level = EventLevel.Warning, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "{0} request to {1} timed out after {2} seconds.")]
        private void HttpRequestTimedOut(
            VssHttpMethod method,
            String url,
            Int32 timeoutInSeconds)
        {
            if (IsEnabled())
            {
                WriteEvent(8, (Int32)method, url, timeoutInSeconds);
            }
        }

        [Event(9, Level = EventLevel.Error, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "{0} request to {1} is not authorized. Details: {2}")]
        private void HttpRequestUnauthorized(
            VssHttpMethod method,
            String url,
            String message)
        {
            if (IsEnabled())
            {
                WriteEvent(9, (Int32)method, url, message);
            }
        }

        [Event(10, Keywords = Keywords.Authentication, Level = EventLevel.Warning, Task = Tasks.HttpRequest, Message = "Authentication failed with status code {0}.%n{1}")]
        private void AuthenticationFailed(
            Int32 statusCode,
            String headers)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(10, statusCode, headers);
            }
        }

        [Event(11, Keywords = Keywords.Authentication, Level = EventLevel.Informational, Task = Tasks.HttpRequest, Message = "Authentication successful using {0} credentials")]
        private void AuthenticationSucceeded(VssCredentialsType credentialsType)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(11, (Int32)credentialsType);
            }
        }

        [Event(12, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Opcode = EventOpcode.Start, Message = "Started authentication")]
        private void AuthenticationStart()
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(12);
            }
        }

        [Event(13, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "Created {0} issued token provider instance {1} ({2})")]
        private void IssuedTokenProviderCreated(
            VssCredentialsType credentialsType,
            Int32 providerId,
            String parameters)
        {
            if (IsEnabled())
            {
                WriteEvent(13, (Int32)credentialsType, providerId, parameters);
            }
        }

        [Event(14, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "Removed {0} issued token provider instance {1} ({2})")]
        private void IssuedTokenProviderRemoved(
            VssCredentialsType credentialsType,
            Int32 providerId,
            String parameters)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(14, (Int32)credentialsType, providerId, parameters);
            }
        }

        [Event(15, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "{0} issued token provider instance {1} is acquiring a token")]
        private void IssuedTokenAcquiring(
            VssCredentialsType credentialsType,
            Int32 providerId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(15, (Int32)credentialsType, providerId);
            }
        }

        [Event(16, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Opcode = EventOpcode.Suspend, Message = "{0} issued token provider instance {1} is waiting for issued token from activity {2}")]
        private void IssuedTokenWaitStart(
            VssCredentialsType credentialsType,
            Int32 providerId,
            Guid waitForActivityId)
        {
            WriteEvent(16, (Int32)credentialsType, providerId, waitForActivityId);
        }

        [Event(17, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Opcode = EventOpcode.Resume, Message = "{0} issued token provider instance {1} received token instance {2}")]
        private void IssuedTokenWaitStop(
            VssCredentialsType credentialsType,
            Int32 providerId,
            Int32 issuedTokenId)
        {
            WriteEvent(17, (Int32)credentialsType, providerId, issuedTokenId);
        }

        [Event(18, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "{0} issued token provider instance {1} acquired new token instance {2}")]
        private void IssuedTokenAcquired(
            VssCredentialsType credentialsType,
            Int32 providerId,
            Int32 issuedTokenId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(18, (Int32)credentialsType, providerId, issuedTokenId);
            }
        }

        [Event(20, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "{0} issued token provider instance {1} invalidated token instance {2}")]
        private void IssuedTokenInvalidated(
            VssCredentialsType credentialsType,
            Int32 providerId,
            Int32 issuedTokenId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(20, (Int32)credentialsType, providerId, issuedTokenId);
            }
        }

        [Event(21, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "{0} issued token provider instance {1} validated token instance {2}")]
        private void IssuedTokenValidated(
            VssCredentialsType credentialsType,
            Int32 providerId,
            Int32 issuedTokenId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(21, (Int32)credentialsType, providerId, issuedTokenId);
            }
        }

        [Event(22, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Message = "{0} issued token provider instance {1} retrieved token instance {2}")]
        private void IssuedTokenRetrievedFromCache(
            VssCredentialsType credentialsType,
            Int32 providerId,
            Int32 issuedTokenId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(22, (Int32)credentialsType, providerId, issuedTokenId);
            }
        }

        [Event(23, Keywords = Keywords.Authentication, Level = EventLevel.Verbose, Task = Tasks.Authentication, Opcode = EventOpcode.Stop, Message = "Finished authentication")]
        private void AuthenticationStop()
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Authentication))
            {
                WriteEvent(23);
            }
        }

        [Event(24, Level = EventLevel.Verbose, Task = Tasks.HttpRequest, Opcode = EventOpcode.Stop, Message = "Finished {0} request to {1} with status code {2}")]
        private void HttpRequestStop(
            VssHttpMethod method,
            String url,
            Int32 statusCode)
        {
            if (IsEnabled())
            {
                WriteEvent(24, (Int32)method, url, statusCode);
            }
        }

        [Event(25, Keywords = Keywords.HttpOperation, Level = EventLevel.Informational, Task = Tasks.HttpOperation, Opcode = EventOpcode.Start, Message = "Starting operation {0}.{1}")]
        private void HttpOperationStart(
            String area,
            String operation)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.HttpOperation))
            {
                WriteEvent(25, area, operation);
            }
        }

        [Event(26, Keywords = Keywords.HttpOperation, Level = EventLevel.Informational, Task = Tasks.HttpOperation, Opcode = EventOpcode.Stop, Message = "Finished operation {0}.{1}")]
        private void HttpOperationStop(
            String area,
            String operation)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.HttpOperation))
            {
                WriteEvent(26, area, operation);
            }
        }

        [Event(27, Keywords = Keywords.Authentication, Level = EventLevel.Error, Task = Tasks.Authentication, Opcode = EventOpcode.Info, Message = "{0} issued token provider instance {1} failed to retrieve a token.%nReason: {2}")]
        private void AuthenticationError(
            VssCredentialsType credentialsType,
            Int32 providerId,
            String message)
        {
            if (IsEnabled(EventLevel.Error, Keywords.Authentication))
            {
                WriteEvent(27, (Int32)credentialsType, providerId, message);
            }
        }

        [Event(28, Keywords = Keywords.Authentication, Level = EventLevel.Warning, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "No issued token provider found which can handle the authentication challenge")]
        private void IssuedTokenProviderNotFound()
        {
            if (IsEnabled(EventLevel.Warning, Keywords.Authentication))
            {
                WriteEvent(28);
            }
        }

        [Event(29, Keywords = Keywords.Authentication, Level = EventLevel.Warning, Task = Tasks.HttpRequest, Opcode = EventOpcode.Info, Message = "{0} issued token provider instance {1} requires an interactive prompt which is not allowed by the current settings")]
        private void IssuedTokenProviderPromptRequired(
            VssCredentialsType credentialsType,
            Int32 providerId)
        {
            if (IsEnabled(EventLevel.Warning, Keywords.Authentication))
            {
                WriteEvent(29, (Int32)credentialsType, providerId);
            }
        }

        [Event(30, Keywords = Keywords.HttpOperation, Level = EventLevel.Critical, Task = Tasks.HttpOperation, Opcode = EventOpcode.Info, Message = "A task completion source was not properly completed during authentication")]
        public void TokenSourceNotCompleted()
        {
            if (IsEnabled(EventLevel.Critical, Keywords.HttpOperation))
            {
                WriteEvent(30);
            }
        }

        [Event(31, Keywords = Keywords.Authentication, Level = EventLevel.Warning, Task = Tasks.Authentication, Opcode = EventOpcode.Info, Message = "Retrieving an AAD auth token took a long time ({0} seconds)")]
        public void AuthorizationDelayed(string timespan)
        {
            if(IsEnabled(EventLevel.Warning, Keywords.Authentication))
            {
                WriteEvent(31, timespan);
            }
        }

        [Event(32, Keywords = Keywords.Authentication, Level = EventLevel.Informational, Task = Tasks.Authentication, Opcode = EventOpcode.Info, Message = "AAD Correlation ID for this token request: {0}")]
        public void AADCorrelationID(string aadCorrelationId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Authentication))
            {
                WriteEvent(32, aadCorrelationId);
            }
        }

        /// <summary>
        /// Sets the activity ID of the current thread.
        /// </summary>
        /// <param name="activity">The trace activity which should be active on the calling thread</param>
        [NonEvent]
        private void SetActivityId(VssTraceActivity activity)
        {
        }

        [NonEvent]
        private static IList<String> SplitMessage(String message)
        {
            List<String> list = new List<String>();
            if (message.Length > 30000)
            {
                int num = 0;
                do
                {
                    Int32 num2 = (message.Length - num > 30000) ? 30000 : (message.Length - num);
                    list.Add(message.Substring(num, num2));
                    num += num2;
                }
                while (message.Length > num);
            }
            else
            {
                list.Add(message);
            }
            return list;
        }

        [NonEvent]
        private void WriteMessageEvent(
            Int32 param0,
            String message,
            Action<Int32, String> writeEvent)
        {
            writeEvent(param0, message);
        }

        [NonEvent]
        private void WriteMessageEvent(
            VssCredentialsType param0,
            Int32 param1,
            String message,
            Action<VssCredentialsType, Int32, String> writeEvent)
        {
            writeEvent(param0, param1, message);
        }

        [NonEvent]
        private void WriteMessageEvent(
            VssHttpMethod param0,
            String param1,
            String message,
            Action<VssHttpMethod, String, String> writeEvent)
        {
            writeEvent(param0, param1, message);
        }

        [NonEvent]
        private new unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            String param1)
        {
            param1 = param1 ?? String.Empty;

            Int32 eventDataCount = 2;
            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = (Int32)(param1.Length + 1) * sizeof(Char);

            fixed (Char* a1 = param1)
            {
                eventData[0].DataPointer = (IntPtr)(&param0);
                eventData[1].DataPointer = (IntPtr)a1;
                base.WriteEventCore(eventId, eventDataCount, eventData);
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            String param1,
            String param2)
        {
            param1 = param1 ?? String.Empty;
            param2 = param2 ?? String.Empty;

            Int32 eventDataCount = 3;
            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = (Int32)(param1.Length + 1) * sizeof(Char);
            eventData[2].Size = (Int32)(param2.Length + 1) * sizeof(Char);

            fixed (Char* a1 = param1, a2 = param2)
            {
                eventData[0].DataPointer = (IntPtr)(&param0);
                eventData[1].DataPointer = (IntPtr)a1;
                eventData[2].DataPointer = (IntPtr)a2;
                base.WriteEventCore(eventId, eventDataCount, eventData);
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            Int32 param1,
            Guid param2)
        {
            Int32 eventDataCount = 3;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = sizeof(Int32);
            eventData[2].Size = sizeof(Guid);
            eventData[0].DataPointer = (IntPtr)(&param0);
            eventData[1].DataPointer = (IntPtr)(&param1);
            eventData[2].DataPointer = (IntPtr)(&param2);
            base.WriteEventCore(eventId, eventDataCount, eventData);
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            Int32 param1,
            String param2)
        {
            param2 = param2 ?? String.Empty;

            Int32 eventDataCount = 3;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = sizeof(Int32);
            eventData[2].Size = (Int32)(param2.Length + 1) * sizeof(Char);
            fixed (Char* a2 = param2)
            {
                eventData[0].DataPointer = (IntPtr)(&param0);
                eventData[1].DataPointer = (IntPtr)(&param1);
                eventData[2].DataPointer = (IntPtr)a2;
                base.WriteEventCore(eventId, eventDataCount, eventData);
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            String param1,
            Int32 param2)
        {
            param1 = param1 ?? String.Empty;

            Int32 eventDataCount = 3;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = (Int32)(param1.Length + 1) * sizeof(Char);
            eventData[2].Size = sizeof(Int32);
            fixed (Char* a1 = param1)
            {
                eventData[0].DataPointer = (IntPtr)(&param0);
                eventData[1].DataPointer = (IntPtr)a1;
                eventData[2].DataPointer = (IntPtr)(&param2);
                base.WriteEventCore(eventId, eventDataCount, eventData);
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            Int32 param1,
            Int32 param2,
            Guid param3)
        {
            Int32 eventDataCount = 4;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = sizeof(Int32);
            eventData[2].Size = sizeof(Int32);
            eventData[3].Size = sizeof(Guid);
            eventData[0].DataPointer = (IntPtr)(&param0);
            eventData[1].DataPointer = (IntPtr)(&param1);
            eventData[2].DataPointer = (IntPtr)(&param2);
            eventData[3].DataPointer = (IntPtr)(&param3);
            base.WriteEventCore(eventId, eventDataCount, eventData);
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            Int32 param1,
            Guid param2,
            Guid param3)
        {
            Int32 eventDataCount = 4;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = sizeof(Int32);
            eventData[2].Size = sizeof(Guid);
            eventData[3].Size = sizeof(Guid);
            eventData[0].DataPointer = (IntPtr)(&param0);
            eventData[1].DataPointer = (IntPtr)(&param1);
            eventData[2].DataPointer = (IntPtr)(&param2);
            eventData[3].DataPointer = (IntPtr)(&param3);
            base.WriteEventCore(eventId, eventDataCount, eventData);
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            String param1,
            Int32 param2,
            String param3)
        {
            param1 = param1 ?? String.Empty;
            param3 = param3 ?? String.Empty;

            Int32 eventDataCount = 4;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = (Int32)(param1.Length + 1) * sizeof(Char);
            eventData[2].Size = sizeof(Int32);
            eventData[3].Size = (Int32)(param3.Length + 1) * sizeof(Char);
            fixed (Char* a1 = param1, a3 = param3)
            {
                eventData[0].DataPointer = (IntPtr)(&param0);
                eventData[1].DataPointer = (IntPtr)a1;
                eventData[2].DataPointer = (IntPtr)(&param2);
                eventData[3].DataPointer = (IntPtr)a3;
                base.WriteEventCore(eventId, eventDataCount, eventData);
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(
            Int32 eventId,
            Int32 param0,
            String param1,
            Int32 param2,
            String param3,
            Double param4)
        {
            param1 = param1 ?? String.Empty;
            param3 = param3 ?? String.Empty;

            Int32 eventDataCount = 5;

            Byte* userData = stackalloc Byte[sizeof(EventData) * eventDataCount];
            EventData* eventData = (EventData*)userData;

            eventData[0].Size = sizeof(Int32);
            eventData[1].Size = (Int32)(param1.Length + 1) * sizeof(Char);
            eventData[2].Size = sizeof(Int32);
            eventData[3].Size = (Int32)(param3.Length + 1) * sizeof(Char);
            eventData[4].Size = sizeof(Double);
            fixed (Char* a1 = param1, a3 = param3)
            {
                eventData[0].DataPointer = (IntPtr)(&param0);
                eventData[1].DataPointer = (IntPtr)a1;
                eventData[2].DataPointer = (IntPtr)(&param2);
                eventData[3].DataPointer = (IntPtr)a3;
                eventData[4].DataPointer = (IntPtr)(&param4);
                base.WriteEventCore(eventId, eventDataCount, eventData);
            }
        }

        private static Lazy<VssHttpEventSource> m_log = new Lazy<VssHttpEventSource>(() => new VssHttpEventSource());
    }

    public static class VssEventSources
    {
        public const String Http = "GitHub-Actions-Http";
    }
}
