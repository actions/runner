using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [ResourceArea(TaskResourceIds.AreaId)]
    public class ActionsRunServerHttpClient : TaskAgentHttpClient
    {
        private static readonly JsonSerializerSettings s_serializerSettings;

        static ActionsRunServerHttpClient()
        {
            s_serializerSettings = new VssJsonMediaTypeFormatter().SerializerSettings;
            s_serializerSettings.DateParseHandling = DateParseHandling.None;
            s_serializerSettings.FloatParseHandling = FloatParseHandling.Double;
        }

        public ActionsRunServerHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public ActionsRunServerHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ActionsRunServerHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ActionsRunServerHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ActionsRunServerHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public Task<Pipelines.AgentJobRequestMessage> GetJobMessageAsync(
            string messageId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("25adab70-1379-4186-be8e-b643061ebe3a");
            object routeValues = new { messageId = messageId };

            return SendAsync<Pipelines.AgentJobRequestMessage>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        protected override async Task<T> ReadJsonContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default(CancellationToken))
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json, s_serializerSettings);
        }
    }
}
