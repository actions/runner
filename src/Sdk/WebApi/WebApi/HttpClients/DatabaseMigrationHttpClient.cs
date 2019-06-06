using System;
using System.Net.Http;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Zeus
{
    [ResourceArea(DatabaseMigrationLocationIds.ResourceString)]
    public class DatabaseMigrationHttpClient : DatabaseMigrationHttpClientBase
    {
        public DatabaseMigrationHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public DatabaseMigrationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public DatabaseMigrationHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public DatabaseMigrationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public DatabaseMigrationHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
    }
}
