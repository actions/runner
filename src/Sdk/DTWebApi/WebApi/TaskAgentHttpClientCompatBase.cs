using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [ResourceArea(TaskResourceIds.AreaId)]
    public abstract class TaskAgentHttpClientCompatBase : VssHttpClientBase
    {
        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TaskAgentHttpClientCompatBase(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            Boolean disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
    }
}
