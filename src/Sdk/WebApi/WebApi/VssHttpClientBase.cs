//-----------------------------------------------------------------------
// <copyright file="VssHttpClientBase.cs" company="Microsoft Corporation">
// Copyright (C) 2009-2014 All Rights Reserved
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using GitHub.Services.Common.Internal;
using GitHub.Services.WebApi.Utilities.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// This class is used as the base class for all the REST client classes.
    /// It wraps a <c>System.Net.Http.HttpClient</c> and sets up standard defaults.
    /// </summary>
    public abstract class VssHttpClientBase : IDisposable
    {
        protected VssHttpClientBase(
            Uri baseUrl,
            VssCredentials credentials)
            : this(baseUrl, credentials, settings: null)
        {
        }

        protected VssHttpClientBase(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : this(baseUrl, credentials, settings: settings, handlers: null)
        {
        }

        protected VssHttpClientBase(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : this(baseUrl, credentials, null, handlers)
        {
        }

        protected VssHttpClientBase(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : this(baseUrl, BuildHandler(credentials, settings, handlers), disposeHandler: true)
        {
        }

        protected VssHttpClientBase(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            bool disposeHandler)
        {
            m_client = new HttpClient(pipeline, disposeHandler);

            // Disable their timeout since we handle it ourselves
            m_client.Timeout = TimeSpan.FromMilliseconds(-1.0);
            m_client.BaseAddress = baseUrl;
            m_formatter = new VssJsonMediaTypeFormatter();

            SetServicePointOptions();

            SetTokenStorageUrlIfNeeded(pipeline);
        }

        private void SetTokenStorageUrlIfNeeded(HttpMessageHandler handler)
        {
            // The TokenStorageUrl should be set by the VssConnection, so that the same
            // url is used for the token storage key regardless of the service this client
            // talks to. If the VssHttpClient is created directly, then the best we can do
            // is to set the storage url to match the base url of the client.
            if (handler is VssHttpMessageHandler vssHttpMessageHandler)
            {
                if (vssHttpMessageHandler.Credentials != null)
                {
                    if (vssHttpMessageHandler.Credentials.Federated != null
                        && vssHttpMessageHandler.Credentials.Federated.TokenStorageUrl == null)
                    {
                        vssHttpMessageHandler.Credentials.Federated.TokenStorageUrl = m_client.BaseAddress;
                    }
                }
            }
            else if (handler is DelegatingHandler delegatingHandler)
            {
                SetTokenStorageUrlIfNeeded(delegatingHandler.InnerHandler);
            }
        }

        private static HttpMessageHandler BuildHandler(VssCredentials credentials, VssHttpRequestSettings settings, DelegatingHandler[] handlers)
        {
            VssHttpMessageHandler innerHandler = new VssHttpMessageHandler(credentials, settings ?? new VssHttpRequestSettings());

            if (null == handlers ||
                0 == handlers.Length)
            {
                return innerHandler;
            }

            return HttpClientFactory.CreatePipeline(innerHandler, handlers);
        }

        /// <summary>
        /// The base address.
        /// </summary>
        public Uri BaseAddress
        {
            get
            {
                return m_client.BaseAddress;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public VssResponseContext LastResponseContext
        {
            get { return m_LastResponseContext; }
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

        /// <summary>
        ///
        /// </summary>
        protected virtual IDictionary<String, Type> TranslatedExceptions
        {
            get
            {
                return null;
            }
        }

        protected HttpResponseMessage Send(
            HttpRequestMessage message,
            Object userState = null)
        {
            try
            {
                var response = SendAsync(message, userState);

                return response.Result;
            }
            catch (AggregateException ag)
            {
                ag = ag.Flatten();
                if (ag.InnerExceptions.Count == 1)
                {
                    throw ag.InnerExceptions[0];
                }
                throw;
            }
        }

        protected Task<HttpResponseMessage> DeleteAsync(
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync(
                /*method:*/ HttpMethod.Delete,
                locationId,
                routeValues,
                version,
                /*content:*/ null,
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<HttpResponseMessage> GetAsync(
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync(
                /*method:*/ HttpMethod.Get,
                locationId,
                routeValues,
                version,
                /*content:*/ null,
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<TResult> GetAsync<TResult>(
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync<TResult>(
                /*method:*/ HttpMethod.Get,
                locationId,
                routeValues,
                version,
                /*content:*/ null,
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<HttpResponseMessage> PatchAsync<T>(
            T value,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync(
                /*method:*/ s_patchMethod.Value,
                locationId,
                routeValues,
                version,
                /*content:*/ new ObjectContent<T>(value, m_formatter),
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<TResult> PatchAsync<T, TResult>(
            T value,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync<TResult>(
                /*method:*/ s_patchMethod.Value,
                locationId,
                routeValues,
                version,
                /*content:*/ new ObjectContent<T>(value, m_formatter),
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<HttpResponseMessage> PostAsync<T>(
            T value,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync(
                /*method:*/ HttpMethod.Post,
                locationId,
                routeValues,
                version,
                /*content:*/ new ObjectContent<T>(value, m_formatter),
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<TResult> PostAsync<T, TResult>(
            T value,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync<TResult>(
                /*method:*/ HttpMethod.Post,
                locationId,
                routeValues,
                version,
                /*content:*/ new ObjectContent<T>(value, m_formatter),
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<HttpResponseMessage> PutAsync<T>(
            T value,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync(
                /*method:*/ HttpMethod.Put,
                locationId,
                routeValues,
                version,
                /*content:*/ new ObjectContent<T>(value, m_formatter),
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<TResult> PutAsync<T, TResult>(
            T value,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SendAsync<TResult>(
                /*method:*/ HttpMethod.Put,
                locationId,
                routeValues,
                version,
                /*content:*/ new ObjectContent<T>(value, m_formatter),
                queryParameters,
                userState,
                cancellationToken);
        }

        protected Task<T> SendAsync<T>(
            HttpMethod method,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return SendAsync<T>(method, null, locationId, routeValues, version, content, queryParameters, userState, cancellationToken);
        }

        protected async Task<T> SendAsync<T>(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(method, additionalHeaders, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false))
            {
                return await SendAsync<T>(requestMessage, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(method, additionalHeaders, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false))
            {
                return await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        protected HttpResponseMessage Send(
            HttpMethod method,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null)
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = CreateRequestMessageAsync(method, locationId, routeValues, version, content, queryParameters, userState, CancellationToken.None).SyncResult())
            {
                return Send(requestMessage, userState);
            }
        }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(method, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false))
            {
                return await SendAsync(requestMessage, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            Guid locationId,
            HttpCompletionOption completionOption,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(method, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false))
            {
                return await SendAsync(requestMessage, completionOption, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create an HTTP request message for the given location, replacing parameters in the location's route template
        /// with values in the supplied routeValues dictionary.
        /// </summary>
        /// <param name="method">HTTP verb to use</param>
        /// <param name="locationId">Id of the location to use</param>
        /// <param name="routeValues">Values to use to replace parameters in the location's route template</param>
        /// <param name="version">Version to send in the request or null to use the VSS latest API version</param>
        /// <param name="mediaType">The mediatype to set in request header.</param>
        /// <returns>HttpRequestMessage</returns>
        protected Task<HttpRequestMessage> CreateRequestMessageAsync(
            HttpMethod method,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            String mediaType = c_jsonMediaType)
        {
            return CreateRequestMessageAsync(method, null, locationId, routeValues, version, content, queryParameters, userState, cancellationToken, mediaType);
        }

        /// <summary>
        /// Create an HTTP request message for the given location, replacing parameters in the location's route template
        /// with values in the supplied routeValues dictionary.
        /// </summary>
        /// <param name="method">HTTP verb to use</param>
        /// <param name="locationId">Id of the location to use</param>
        /// <param name="routeValues">Values to use to replace parameters in the location's route template</param>
        /// <param name="version">Version to send in the request or null to use the VSS latest API version</param>
        /// <param name="mediaType">The mediatype to set in request header.</param>
        /// <returns>HttpRequestMessage</returns>
        protected virtual async Task<HttpRequestMessage> CreateRequestMessageAsync(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            Guid locationId,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken),
            String mediaType = c_jsonMediaType)
        {
            // Lookup the location
            ApiResourceLocation location = await GetResourceLocationAsync(locationId, userState, cancellationToken).ConfigureAwait(false);
            if (location == null)
            {
                throw new VssResourceNotFoundException(locationId, BaseAddress);
            }

            return CreateRequestMessage(method, additionalHeaders, location, routeValues, version, content, queryParameters, mediaType);
        }

        /// <summary>
        /// Create an HTTP request message for the given location, replacing parameters in the location's route template
        /// with values in the supplied routeValues dictionary.
        /// </summary>
        /// <param name="method">HTTP verb to use</param>
        /// <param name="location">API resource location</param>
        /// <param name="routeValues">Values to use to replace parameters in the location's route template</param>
        /// <param name="version">Version to send in the request or null to use the VSS latest API version</param>
        /// <param name="mediaType">The mediatype to set in request header.</param>
        /// <returns>HttpRequestMessage</returns>
        protected HttpRequestMessage CreateRequestMessage(
            HttpMethod method,
            ApiResourceLocation location,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            String mediaType = c_jsonMediaType)
        {
            return CreateRequestMessage(method, null, location, routeValues, version, content, queryParameters, mediaType);
        }

        /// <summary>
        /// Create an HTTP request message for the given location, replacing parameters in the location's route template
        /// with values in the supplied routeValues dictionary.
        /// </summary>
        /// <param name="method">HTTP verb to use</param>
        /// <param name="location">API resource location</param>
        /// <param name="routeValues">Values to use to replace parameters in the location's route template</param>
        /// <param name="version">Version to send in the request or null to use the VSS latest API version</param>
        /// <param name="mediaType">The mediatype to set in request header.</param>
        /// <returns>HttpRequestMessage</returns>
        protected HttpRequestMessage CreateRequestMessage(
            HttpMethod method,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders,
            ApiResourceLocation location,
            Object routeValues = null,
            ApiResourceVersion version = null,
            HttpContent content = null,
            IEnumerable<KeyValuePair<String, String>> queryParameters = null,
            String mediaType = c_jsonMediaType)
        {
            CheckForDisposed();
            // Negotiate the request version to send
            ApiResourceVersion requestVersion = NegotiateRequestVersion(location, version);
            if (requestVersion == null)
            {
                throw new VssVersionNotSupportedException(location, version.ApiVersion, location.MinVersion, BaseAddress);
            }

            // Construct the url
            Dictionary<String, Object> valuesDictionary = VssHttpUriUtility.ToRouteDictionary(routeValues, location.Area, location.ResourceName);

            String locationRelativePath = VssHttpUriUtility.ReplaceRouteValues(location.RouteTemplate, valuesDictionary);
            Uri locationUri = VssHttpUriUtility.ConcatUri(BaseAddress, locationRelativePath);
            if (queryParameters != null && queryParameters.Any())
            {
                locationUri = locationUri.AppendQuery(queryParameters);
            }

            // Create the message and populate headers
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, locationUri.AbsoluteUri);

            MediaTypeWithQualityHeaderValue acceptType = CreateAcceptHeader(requestVersion, mediaType);

            if (m_excludeUrlsHeader)
            {
                acceptType.Parameters.Add(new NameValueHeaderValue(VssHttpRequestSettings.ExcludeUrlsHeader, "true"));
            }
            if (m_lightweightHeader)
            {
                acceptType.Parameters.Add(new NameValueHeaderValue(VssHttpRequestSettings.LightweightHeader, "true"));
            }

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
                if (requestMessage.Content.Headers.ContentType != null && !requestMessage.Content.Headers.ContentType.Parameters.Any(p => p.Name.Equals(ApiResourceVersionExtensions.c_apiVersionHeaderKey)))
                {
                    // add the api-version to the content header, which will be used by the JsonCompatConverter to know which version of the model to convert to.
                    requestMessage.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue(ApiResourceVersionExtensions.c_apiVersionHeaderKey, requestVersion.ToString()));
                }
            }

            return requestMessage;
        }

        protected virtual MediaTypeWithQualityHeaderValue CreateAcceptHeader(ApiResourceVersion requestVersion, String mediaType)
        {
            MediaTypeWithQualityHeaderValue acceptType = new MediaTypeWithQualityHeaderValue(mediaType);
            acceptType.Parameters.AddApiResourceVersionValues(requestVersion, replaceExisting: true, useLegacyFormat: requestVersion.ApiVersion.Major <= 1);
            return acceptType;
        }

        protected virtual void AddModelAsQueryParams(IList<KeyValuePair<String, String>> queryParams, string parameterName, object model)
        {
            JObject jObject = JObject.FromObject(model, new VssJsonMediaTypeFormatter().CreateJsonSerializer());
            AddModelAsQueryParams(queryParams, parameterName, jObject);
        }

        protected virtual void AddIEnumerableAsQueryParams(IList<KeyValuePair<String, String>> queryParams, string parameterName, object model)
        {
            JArray jArray = JArray.FromObject(model, new VssJsonMediaTypeFormatter().CreateJsonSerializer());
            AddModelAsQueryParams(queryParams, parameterName, jArray);
        }

        private void AddModelAsQueryParams(IList<KeyValuePair<String, String>> queryParams, string parameterName, JObject jObject)
        {
            foreach (JProperty property in jObject.Properties())
            {
                AddModelAsQueryParams(queryParams, parameterName, property);
            }
        }

        private void AddModelAsQueryParams(IList<KeyValuePair<String, String>> queryParams, string key, JProperty property)
        {
            if (property.Value != null)
            {
                string newKey = string.Format("{0}[{1}]", key, property.Name);
                AddModelAsQueryParams(queryParams, newKey, property.Value);
            }
        }

        private void AddModelAsQueryParams(IList<KeyValuePair<String, String>> queryParams, string key, JArray array)
        {
            int i = 0;
            foreach (JToken childToken in array.Children())
            {
                string newKey = string.Format("{0}[{1}]", key, i);
                AddModelAsQueryParams(queryParams, newKey, childToken);
                i++;
            }
        }

        private void AddModelAsQueryParams(IList<KeyValuePair<String, String>> queryParams, string key, JToken token)
        {
            if (token.Type == JTokenType.Array)
            {
                AddModelAsQueryParams(queryParams, key, (JArray)token);
            }
            else if (token.Type == JTokenType.Object)
            {
                AddModelAsQueryParams(queryParams, key, (JObject)token);
            }
            else if (token.Type == JTokenType.Property)
            {
                AddModelAsQueryParams(queryParams, key, (JProperty)token);
            }
            else if (token.Type == JTokenType.Date)
            {
                AddDateTimeToQueryParams(queryParams, key, (DateTime)token);
            }
            else
            {
                queryParams.Add(key, token.ToString());
            }
        }

        /// <summary>
        /// Ensures we are using a standard format for sending DateTime value as a query parameter (o: 2015-02-16T16:11:31.1398684Z)
        /// </summary>
        /// <param name="queryParams"></param>
        /// <param name="name"></param>
        /// <param name="localDateTime">local DateTime value</param>
        protected void AddDateTimeToQueryParams(IList<KeyValuePair<String, String>> queryParams, String name, DateTime localDateTime)
        {
            // converting to universal time to match json serialization server is using
            queryParams.Add(name, localDateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Ensures we are using a standard format for sending DateTime value as a query parameter (o: 2015-02-16T16:11:31.1398684Z)
        /// </summary>
        /// <param name="queryParams"></param>
        /// <param name="name"></param>
        /// <param name="dateTimeOffset"></param>
        protected void AddDateTimeToQueryParams(IList<KeyValuePair<String, String>> queryParams, String name, DateTimeOffset dateTimeOffset)
        {
            queryParams.Add(name, dateTimeOffset.ToString("o", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Ensures we are using a standard format (HTTP-date) for sending DateTime value as header
        /// (r: Wed, 1 Jan 2016 18:43:31 GMT) per W3C specification.
        /// </summary>
        /// <param name="queryParams"></param>
        /// <param name="name"></param>
        /// <param name="dateTimeOffset"></param>
        protected void AddDateTimeToHeaders(IList<KeyValuePair<String, String>> queryParams, String name, DateTimeOffset dateTimeOffset)
        {
            queryParams.Add(name, dateTimeOffset.ToString("r", CultureInfo.InvariantCulture));
        }

        protected async Task<T> SendAsync<T>(
            HttpRequestMessage message,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            //ConfigureAwait(false) enables the continuation to be run outside
            //any captured SyncronizationContext (such as ASP.NET's) which keeps things
            //from deadlocking...
            using (HttpResponseMessage response = await this.SendAsync(message, userState, cancellationToken).ConfigureAwait(false))
            {
                return await ReadContentAsAsync<T>(response, cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task<T> ReadContentAsAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            Boolean isJson = IsJsonResponse(response);
            bool mismatchContentType = false;
            try
            {
                //deal with wrapped collections in json
                if (isJson &&
                    typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) &&
                    !typeof(Byte[]).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) &&
                    !typeof(JObject).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
                {
                    // expect it to come back wrapped, if it isn't it is a bug!
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
                // In this case, do nothing and utilize the HandleUnknownContentType call below
                mismatchContentType = true;
            }

            if (HasContent(response))
            {
                return await HandleInvalidContentType<T>(response, mismatchContentType).ConfigureAwait(false);
            }
            else
            {
                return default(T);
            }
        }

        protected virtual async Task<T> ReadJsonContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await response.Content.ReadAsAsync<T>(new[] { m_formatter }, cancellationToken).ConfigureAwait(false);
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
                if (userState != null)
                {
                    message.Properties[UserStatePropertyName] = userState;
                }
                
                if (!message.Headers.Contains(Common.Internal.HttpHeaders.VssE2EID))
                {
                    message.Headers.Add(Common.Internal.HttpHeaders.VssE2EID, Guid.NewGuid().ToString("D"));
                }
                VssHttpEventSource.Log.HttpRequestStart(traceActivity, message);
                message.Trace();
                message.Properties[VssTraceActivity.PropertyName] = traceActivity;

                // Send the completion option to the inner handler stack so we know when it's safe to buffer
                // and when we should avoid buffering.
                message.Properties[VssHttpRequestSettings.HttpCompletionOptionPropertyName] = completionOption;

                //ConfigureAwait(false) enables the continuation to be run outside
                //any captured SyncronizationContext (such as ASP.NET's) which keeps things
                //from deadlocking...
                HttpResponseMessage response = await Client.SendAsync(message, completionOption, cancellationToken).ConfigureAwait(false);

                // Inject delay or failure for testing
                if (TestDelay != TimeSpan.Zero)
                {
                    await ProcessDelayAsync().ConfigureAwait(false);
                }

                await HandleResponseAsync(response, cancellationToken).ConfigureAwait(false);

                return response;
            }
        }

        [Obsolete("Use VssHttpClientBase.HandleResponseAsync instead")]
        protected virtual void HandleResponse(HttpResponseMessage response)
        {

        }

        protected virtual async Task HandleResponseAsync(
            HttpResponseMessage response, 
            CancellationToken cancellationToken)
        {
            response.Trace();
            VssHttpEventSource.Log.HttpRequestStop(VssTraceActivity.Current, response);

            m_LastResponseContext = new VssResponseContext(response.StatusCode, response.Headers);

            if (response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
            {
                throw (m_LastResponseContext.Exception = new ProxyAuthenticationRequiredException());
            }
            else if (ShouldThrowError(response))
            {
                Exception exToThrow = null;
                if (IsJsonResponse(response))
                {
                    exToThrow = await UnwrapExceptionAsync(response.Content, cancellationToken).ConfigureAwait(false);
                }

                if (exToThrow == null || !(exToThrow is VssException))
                {
                    String message = null;
                    if (exToThrow != null)
                    {
                        message = exToThrow.Message;
                    }

                    IEnumerable<String> serviceError;
                    if (response.Headers.TryGetValues(Common.Internal.HttpHeaders.TfsServiceError, out serviceError))
                    {
                        message = UriUtility.UrlDecode(serviceError.FirstOrDefault());
                    }
                    else if (String.IsNullOrEmpty(message) && !String.IsNullOrEmpty(response.ReasonPhrase))
                    {
                        message = response.ReasonPhrase;
                    }
                    exToThrow = new VssServiceResponseException(response.StatusCode, message, exToThrow);
                }

                m_LastResponseContext.Exception = exToThrow;
                throw exToThrow;
            }
        }

        protected async Task<Exception> UnwrapExceptionAsync(HttpContent content, CancellationToken cancellationToken)
        {
            WrappedException wrappedException = await content.ReadAsAsync<WrappedException>(new MediaTypeFormatter[] { m_formatter }, cancellationToken).ConfigureAwait(false);
            return wrappedException.Unwrap(this.TranslatedExceptions);
        }

        protected virtual bool ShouldThrowError(HttpResponseMessage response)
        {
            return !response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Negotiate the appropriate request version to use for the given api resource location, based on
        /// the client and server capabilities
        /// </summary>
        /// <param name="locationId">Id of the API resource location</param>
        /// <param name="version">Client version to attempt to use (use the latest VSS API version if unspecified)</param>
        /// <returns>Max API version supported on the server that is less than or equal to the client version. Returns null if the server does not support this location or this version of the client.</returns>
        protected async Task<ApiResourceVersion> NegotiateRequestVersionAsync(
            Guid locationId,
            ApiResourceVersion version = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ApiResourceLocation location = await GetResourceLocationAsync(locationId, userState, cancellationToken).ConfigureAwait(false);
            if (location == null)
            {
                return null;
            }
            else
            {
                return NegotiateRequestVersion(location, version);
            }
        }

        /// <summary>
        /// Negotiate the appropriate request version to use for the given api resource location, based on
        /// the client and server capabilities
        /// </summary>
        /// <param name="location">Location of the API resource</param>
        /// <param name="version">Client version to attempt to use (use the latest VSS API version if unspecified)</param>
        /// <returns>Max API version supported on the server that is less than or equal to the client version. Returns null if the server does not support this location or this version of the client.</returns>
        protected ApiResourceVersion NegotiateRequestVersion(
            ApiResourceLocation location,
            ApiResourceVersion version = null)
        {
            if (version == null)
            {
                version = m_defaultApiVersion;
            }

            if (location.MinVersion > version.ApiVersion)
            {
                // Client is older than the server. The server no longer supports this resource (deprecated).
                return null;
            }
            else if (location.MaxVersion < version.ApiVersion)
            {
                // Client is newer than the server. Negotiate down to the latest version on the server
                ApiResourceVersion negotiatedVersion = new ApiResourceVersion(location.MaxVersion, 0);
                negotiatedVersion.IsPreview = location.ReleasedVersion < location.MaxVersion;
                return negotiatedVersion;
            }
            else
            {
                // We can send at the requested api version. Make sure the resource version is not bigger than what the server supports
                int resourceVersion = Math.Min(version.ResourceVersion, location.ResourceVersion);
                ApiResourceVersion negotiatedVersion = new ApiResourceVersion(version.ApiVersion, resourceVersion);
                if (location.ReleasedVersion < version.ApiVersion)
                {
                    negotiatedVersion.IsPreview = true;
                }
                else
                {
                    negotiatedVersion.IsPreview = version.IsPreview;
                }
                return negotiatedVersion;
            }
        }

        /// <summary>
        /// Sets the ApiResourceLocationCollection for this VssHttpClientBase.
        /// If unset and needed, the data will be fetched through an OPTIONS request.
        /// </summary>
        public void SetResourceLocations(ApiResourceLocationCollection resourceLocations)
        {
            if (null == m_resourceLocations)
            {
                m_resourceLocations = resourceLocations;
            }
        }

        /// <summary>
        /// Adds the excludeUrls=true accept header to the requests generated by this client. 
        /// If respected by the server, urls will not be included in the responses.
        /// </summary>
        public bool ExcludeUrlsHeader
        {
            get
            {
                return m_excludeUrlsHeader;
            }

            set
            {
                m_excludeUrlsHeader = value;
            }
        }


        /// <summary>
        /// Add the lightWeight=true option to the accept header in the requests generated by this client. 
        /// If respected by the server, light weight responses carrying only basic metadata information
        /// will be returned and urls will be excluded. 
        /// </summary>
        public bool LightweightHeader
        {
            get
            {
                return m_lightweightHeader;
            }
            set
            {
                m_lightweightHeader = value;
            }
        }

        /// <summary>
        /// Get information about an API resource location by its location id
        /// </summary>
        /// <param name="locationId">Id of the API resource location</param>
        /// <returns></returns>
        protected async Task<ApiResourceLocation> GetResourceLocationAsync(
            Guid locationId,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            await EnsureResourceLocationsPopulated(userState, cancellationToken).ConfigureAwait(false);
            return m_resourceLocations.TryGetLocationById(locationId);
        }

        internal virtual async Task<IEnumerable<ApiResourceLocation>> GetResourceLocationsAsync(
                            Boolean allHostTypes,
                            Object userState = null,
                            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            // Send an Options request to retrieve all api resource locations if the collection of resource locations is not populated.
            Uri optionsUri = VssHttpUriUtility.ConcatUri(BaseAddress, allHostTypes ? c_optionsRelativePathWithAllHostTypes : c_optionsRelativePath);
            using (HttpRequestMessage optionsRequest = new HttpRequestMessage(HttpMethod.Options, optionsUri))
            {
                return await SendAsync<IEnumerable<ApiResourceLocation>>(optionsRequest, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task EnsureResourceLocationsPopulated(
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (m_resourceLocations == null)
            {
                // Send an Options request to retrieve all api resource locations if the collection of resource locations is not populated.
                Uri optionsUri = VssHttpUriUtility.ConcatUri(BaseAddress, c_optionsRelativePath);
                IEnumerable<ApiResourceLocation> locations = await GetResourceLocationsAsync(allHostTypes: false, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
                ApiResourceLocationCollection resourceLocations = new ApiResourceLocationCollection();
                resourceLocations.AddResourceLocations(locations);
                m_resourceLocations = resourceLocations;
            }
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

        private async Task<T> HandleInvalidContentType<T>(HttpResponseMessage response, bool isMismatchedContentType)
        {
            //the response is not Json, cannot read it with Json formatter, get the string and throw an exception
            String responseType = response.Content?.Headers?.ContentType?.MediaType ?? "Unknown";
            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                using (var streamReader = new StreamReader(responseStream))
                {
                    //read at most 4K
                    const int oneK = 1024;
                    char[] contentBuffer = new char[4 * oneK];
                    int contentLength = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        int read = await streamReader.ReadAsync(contentBuffer, i * oneK, oneK).ConfigureAwait(false);
                        contentLength += read;
                        if (read < oneK) break;
                    }

                    string responseText;
                    if (isMismatchedContentType)
                    {
                        responseText = $"Mismatched response content type. {responseType} Response Content: {new String(contentBuffer, 0, contentLength)}";
                    }
                    else
                    {
                        responseText = $"Invalid response content type: {responseType} Response Content: {new String(contentBuffer, 0, contentLength)}";
                    }

                    throw new VssServiceResponseException(response.StatusCode, responseText, null);
                }
            }
        }

        private void SetServicePointOptions()
        {
            if (BaseAddress != null)
            {
                ServicePoint servicePoint = ServicePointManager.FindServicePoint(BaseAddress);
                servicePoint.UseNagleAlgorithm = false;
                servicePoint.SetTcpKeepAlive(
                    enabled: true,
                    keepAliveTime: c_keepAliveTime,
                    keepAliveInterval: c_keepAliveInterval);
            }
        }

        // ServicePoint defaults
        private const int c_keepAliveTime = 30000;
        private const int c_keepAliveInterval = 5000;

        #region IDisposable Support
        private bool m_isDisposed = false;
        private object m_disposeLock = new object();

        [Obsolete("This overload of Dispose has been deprecated.  Use the Dispose() method.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Dispose();
            }
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

        private void CheckForDisposed()
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
        #endregion

        protected IEnumerable<String> GetHeaderValue(HttpResponseMessage response, string headerName)
        {
            IEnumerable<string> headerValue;
            if (!response.Headers.TryGetValues(headerName, out headerValue))
            {
                if (response.Content != null)
                {
                    response.Content.Headers.TryGetValues(headerName, out headerValue);
                }
            }
            return headerValue;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TimeSpan TestDelay { get; set; }

        private async Task ProcessDelayAsync()
        {
            await Task.Delay(Math.Abs((Int32)TestDelay.TotalMilliseconds)).ConfigureAwait(false);
            if (TestDelay < TimeSpan.Zero)
            {
                throw new Exception("User injected failure.");
            }
        }

        /// <summary>
        /// Internal for testing only.
        /// </summary>
        internal bool HasResourceLocations
        {
            get
            {
                return m_resourceLocations != null;
            }
        }

        private readonly HttpClient m_client;
        private MediaTypeFormatter m_formatter;
        private VssResponseContext m_LastResponseContext;
        private ApiResourceLocationCollection m_resourceLocations;
        private ApiResourceVersion m_defaultApiVersion = new ApiResourceVersion(1.0);

        /// <summary>
        /// Client option to suppress the generation of links in the responses for the requests made by this client. 
        /// If set, "excludeUrls=true" will be appended to the Accept header of the request. 
        /// </summary> 
        private bool m_excludeUrlsHeader;

        /// <summary>
        /// Client option to generate lightweight responses that carry only basic metadata information for the 
        /// requests made by this client. Links should not be generated either. 
        /// If set, "lightweight=true" will be appended to the Accept header of the request.
        /// </summary>
        private bool m_lightweightHeader;

        private Lazy<HttpMethod> s_patchMethod = new Lazy<HttpMethod>(() => new HttpMethod("PATCH"));

        /// <summary>
        /// This is only needed for the Options request that we are making right now. Eventually
        /// we will use the Location Service and the Options request will not be needed and we can remove this.
        /// </summary>
        private const String c_optionsRelativePath = "_apis/";

        private const String c_optionsRelativePathWithAllHostTypes = "_apis/?allHostTypes=true";

        private const String c_jsonMediaType = "application/json";

        public readonly static String UserStatePropertyName = "VssClientBaseUserState";

        protected sealed class OperationScope : IDisposable
        {
            public OperationScope(
                String area,
                String operation)
            {
                m_area = area;
                m_operation = operation;
                m_activity = VssTraceActivity.GetOrCreate();
                m_correlationScope = m_activity.EnterCorrelationScope();
                VssHttpEventSource.Log.HttpOperationStart(m_activity, m_area, operation);
            }

            public void Dispose()
            {
                if (!m_disposed)
                {
                    m_disposed = true;
                    VssHttpEventSource.Log.HttpOperationStop(m_activity, m_area, m_operation);

                    if (m_correlationScope != null)
                    {
                        m_correlationScope.Dispose();
                        m_correlationScope = null;
                    }
                }
            }

            private String m_area;
            private String m_operation;
            private Boolean m_disposed;
            private VssTraceActivity m_activity;
            private IDisposable m_correlationScope;
        }

    }
}
