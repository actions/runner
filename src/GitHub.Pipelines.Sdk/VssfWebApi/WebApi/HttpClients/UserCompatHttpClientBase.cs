using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Users.Client
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class UserCompatHttpClientBase : VssHttpClientBase
    {
        public UserCompatHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public UserCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public UserCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public UserCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public UserCompatHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        protected virtual Task<User> GetUserAsync(
            string descriptor,
            Boolean? createIfNotExists,
            SubjectDescriptor? knownDescriptor,
            object userState,
            CancellationToken cancellationToken)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("61117502-a055-422c-9122-b56e6643ed02");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
            if (createIfNotExists != null)
            {
                additionalHeaders.Add("X-VSS-FaultInUser", createIfNotExists.Value.ToString());
            }
            if (knownDescriptor != null)
            {
                additionalHeaders.Add("X-VSS-KnownDescriptor", knownDescriptor.Value.ToString());
            }

            return SendAsync<User>(
                httpMethod,
                additionalHeaders,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.0, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
