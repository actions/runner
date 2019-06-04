using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Commerce.Client
{
    [ResourceArea(CommerceResourceIds.AreaId)]
    public class ConnectedServerHttpClient : VssHttpClientBase
    {
        #region Constructors

        public ConnectedServerHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

         public ConnectedServerHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ConnectedServerHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ConnectedServerHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ConnectedServerHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual  Task<ConnectedServer> CreateConnectedServer(ConnectedServer server, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = CommerceResourceIds.ConnectedServerLocationId;
            HttpContent content = new ObjectContent<ConnectedServer>(server, new VssJsonMediaTypeFormatter(true));

            return SendAsync<ConnectedServer>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("3.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }

        public virtual  Task<ConnectedServer> ConnectConnectedServer(ConnectedServer server, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = CommerceResourceIds.ConnectedServerLocationId;
            HttpContent content = new ObjectContent<ConnectedServer>(server, new VssJsonMediaTypeFormatter(true));

            return SendAsync<ConnectedServer>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("3.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }

        #endregion

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        internal static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            // 400 - Bad Request    
            {"InvalidResourceException", typeof(InvalidResourceException)},

            // 401 - Unauthorized
            {"CommerceSecurityException", typeof(CommerceSecurityException)},
        };

        protected static readonly Version previewApiVersion = new Version(3, 0);

    }
}
