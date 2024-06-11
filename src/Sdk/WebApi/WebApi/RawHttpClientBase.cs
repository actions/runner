using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Utilities.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sdk.WebApi.WebApi
{
    public class RawHttpClientBase : IDisposable
    {
        protected RawHttpClientBase(
            Uri baseUrl,
            VssOAuthCredential credentials)
            : this(baseUrl, credentials, settings: null)
        {
        }

        protected RawHttpClientBase(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings)
            : this(baseUrl, credentials, settings: settings, handlers: null)
        {
        }

        protected RawHttpClientBase(
            Uri baseUrl,
            VssOAuthCredential credentials,
            params DelegatingHandler[] handlers)
            : this(baseUrl, credentials, null, handlers)
        {
        }

        protected RawHttpClientBase(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : this(baseUrl, BuildHandler(credentials, settings, handlers), disposeHandler: true)
        {
        }

        protected RawHttpClientBase(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            bool disposeHandler)
        {
            m_client = new HttpClient(pipeline, disposeHandler);

            // Disable their timeout since we handle it ourselves
            m_client.Timeout = TimeSpan.FromMilliseconds(-1.0);
            m_client.BaseAddress = baseUrl;
            m_formatter = new VssJsonMediaTypeFormatter();
        }

        public void Dispose()
        {
            if (!m_isDisposed)
            {
                lock (m_disposeLock)
                {
                    if (!m_isDisposed)
                    {
                        m_isDisposed = true;
                        m_client.Dispose();
                    }
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TimeSpan TestDelay { get; set; }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            Uri requestUri,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = CreateRequestMessage(method, null, requestUri, content, queryParameters))
            {
                return await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        protected Task<RawHttpClientResult<T>> SendAsync<T>(
            HttpMethod method,
            Uri requestUri,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Boolean readErrorContent = false,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return SendAsync<T>(method, null, requestUri, content, queryParameters, readErrorContent, userState, cancellationToken);
        }

        protected async Task<RawHttpClientResult<T>> SendAsync<T>(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            Uri requestUri,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Boolean readErrorContent = false,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = CreateRequestMessage(method, additionalHeaders, requestUri, content, queryParameters))
            {
                return await SendAsync<T>(requestMessage, readErrorContent, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task<RawHttpClientResult<T>> SendAsync<T>(
            HttpRequestMessage message,
            Boolean readErrorContent = false,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            //ConfigureAwait(false) enables the continuation to be run outside
            //any captured SyncronizationContext (such as ASP.NET's) which keeps things
            //from deadlocking...
            using (HttpResponseMessage response = await this.SendAsync(message, userState, cancellationToken).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    T data = await ReadContentAsAsync<T>(response, cancellationToken).ConfigureAwait(false);
                    return RawHttpClientResult<T>.Ok(data);
                }
                else
                {
                    var errorContent = default(string);
                    if (readErrorContent)
                    {
                        errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    }

                    string errorMessage = $"Error: {response.ReasonPhrase}";
                    return RawHttpClientResult<T>.Fail(errorMessage, response.StatusCode, errorContent);
                }
            }
        }

        protected Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage message,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // the default in httpClient for HttpCompletionOption is ResponseContentRead so that is what we do here
            return this.SendAsync(
                message,
                /*completionOption:*/ HttpCompletionOption.ResponseContentRead,
                userState,
                cancellationToken);
        }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage message,
            HttpCompletionOption completionOption,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            if (message.Headers.UserAgent != null)
            {
                foreach (ProductInfoHeaderValue headerValue in UserAgentUtility.GetDefaultRestUserAgent())
                {
                    if (!message.Headers.UserAgent.Contains(headerValue))
                    {
                        message.Headers.UserAgent.Add(headerValue);
                    }
                }
            }

            VssTraceActivity traceActivity = VssTraceActivity.GetOrCreate();
            using (traceActivity.EnterCorrelationScope())
            {
                VssHttpEventSource.Log.HttpRequestStart(traceActivity, message);
                message.Trace();
                HttpResponseMessage response = await Client.SendAsync(message, completionOption, cancellationToken)
                    .ConfigureAwait(false);

                // Inject delay or failure for testing
                if (TestDelay != TimeSpan.Zero)
                {
                    await ProcessDelayAsync().ConfigureAwait(false);
                }

                response.Trace();
                VssHttpEventSource.Log.HttpRequestStop(VssTraceActivity.Current, response);

                return response;
            }
        }

        protected async Task<T> ReadContentAsAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            Boolean isJson = IsJsonResponse(response);
            try
            {
                //deal with wrapped collections in json
                if (isJson &&
                    typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) &&
                    !typeof(Byte[]).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) &&
                    !typeof(JObject).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
                {
                    var wrapper = await ReadJsonContentAsync<VssJsonCollectionWrapper<T>>(response, cancellationToken).ConfigureAwait(false);
                    return wrapper.Value;
                }
                else if (isJson)
                {
                    return await ReadJsonContentAsync<T>(response, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (JsonReaderException)
            {
                // We thought the content was JSON but failed to parse.
                // We ignore for now
            }

            return default(T);
        }

        protected virtual async Task<T> ReadJsonContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await response.Content.ReadAsAsync<T>(new[] { m_formatter }, cancellationToken).ConfigureAwait(false);
        }

        protected HttpRequestMessage CreateRequestMessage(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            Uri requestUri,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            String mediaType = c_jsonMediaType)
        {
            CheckForDisposed();
            if (queryParameters != null && queryParameters.Any())
            {
                requestUri = requestUri.AppendQuery(queryParameters);
            }

            HttpRequestMessage requestMessage = new HttpRequestMessage(method, requestUri.AbsoluteUri);

            MediaTypeWithQualityHeaderValue acceptType = new MediaTypeWithQualityHeaderValue(mediaType);
            requestMessage.Headers.Accept.Add(acceptType);
            if (additionalHeaders != null)
            {
                foreach (KeyValuePair<String, String> kvp in additionalHeaders)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            if (content != null)
            {
                requestMessage.Content = content;
            }

            return requestMessage;
        }

        /// <summary>
        /// The inner client.
        /// </summary>
        /// <remarks>
        /// Note to implementers: You should not update or expose the inner client
        /// unless you instantiate your own instance of this class. Getting
        /// an instance of this class from method such as GetClient&lt;T&gt;
        /// a cached and shared instance.
        /// </remarks>
        protected HttpClient Client
        {
            get
            {
                return m_client;
            }
        }

        /// <summary>
        /// The media type formatter.
        /// </summary>
        /// <remarks>
        /// Note to implementers: You should not update or expose the media type formatter
        /// unless you instantiate your own instance of this class. Getting
        /// an instance of this class from method such as GetClient&lt;T&gt;
        /// a cached and shared instance.
        /// </remarks>
        protected MediaTypeFormatter Formatter
        {
            get
            {
                return m_formatter;
            }
        }

        private static HttpMessageHandler BuildHandler(VssOAuthCredential credentials, RawClientHttpRequestSettings settings, DelegatingHandler[] handlers)
        {
            RawHttpMessageHandler innerHandler = new RawHttpMessageHandler(credentials, settings ?? new RawClientHttpRequestSettings());

            if (null == handlers ||
                0 == handlers.Length)
            {
                return innerHandler;
            }

            return HttpClientFactory.CreatePipeline(innerHandler, handlers);
        }

        private void CheckForDisposed()
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        private async Task ProcessDelayAsync()
        {
            await Task.Delay(Math.Abs((Int32)TestDelay.TotalMilliseconds)).ConfigureAwait(false);
            if (TestDelay < TimeSpan.Zero)
            {
                throw new Exception("User injected failure.");
            }
        }

        private Boolean IsJsonResponse(
            HttpResponseMessage response)
        {
            if (HasContent(response)
                && response.Content.Headers != null && response.Content.Headers.ContentType != null
                && !String.IsNullOrEmpty(response.Content.Headers.ContentType.MediaType))
            {
                return (0 == String.Compare("application/json", response.Content.Headers.ContentType.MediaType, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        private Boolean HasContent(HttpResponseMessage response)
        {
            if (response != null &&
                response.StatusCode != HttpStatusCode.NoContent &&
                response.RequestMessage?.Method != HttpMethod.Head &&
                response.Content?.Headers != null &&
                (!response.Content.Headers.ContentLength.HasValue ||
                 (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength != 0)))
            {
                return true;
            }

            return false;
        }

        private readonly HttpClient m_client;
        private MediaTypeFormatter m_formatter;
        private bool m_isDisposed = false;
        private object m_disposeLock = new object();
        private const String c_jsonMediaType = "application/json";
    }
}
