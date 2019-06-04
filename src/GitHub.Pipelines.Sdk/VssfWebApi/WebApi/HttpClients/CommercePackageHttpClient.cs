using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Account;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Commerce.Client
{
    /// <summary>
    /// Class that represents methods communicating with the platform service via REST controller.
    /// </summary>
    [ResourceArea(CommerceResourceIds.AreaId)]
    public class CommercePackageHttpClient : VssHttpClientBase
    {
        #region Constructors

        public CommercePackageHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public CommercePackageHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public CommercePackageHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public CommercePackageHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public CommercePackageHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        protected static readonly Version currentApiVersion = new Version(3, 0);


        /// <summary>
        /// Returns the package of offer subscriptions and meter
        /// </summary>
        /// <returns>A package of signed offer meters and subscriptions</returns>
        [Obsolete]
        public virtual async Task<CommercePackage> GetCommercePackage(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetCommercePackage"))
            {
                return await GetAsync<CommercePackage>(
                    locationId: CommerceResourceIds.CommercePackageLocationId,
                    userState: userState,
                    version: new ApiResourceVersion(currentApiVersion, CommerceResourceVersions.CommercePackageV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the package of offer subscriptions and meter
        /// </summary>
        /// <returns>A package of signed offer meters and subscriptions</returns>
        public virtual async Task<CommercePackage> GetCommercePackage(string version, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetCommercePackage"))
            {
                var queryParameters = new List<KeyValuePair<string, string>>
                {
                    { "version", version }
                };
                return await GetAsync<CommercePackage>(
                    locationId: CommerceResourceIds.CommercePackageLocationId,
                    userState: userState,
                    version: new ApiResourceVersion(currentApiVersion, CommerceResourceVersions.CommercePackageV1Resources),
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        [ExcludeFromCodeCoverage]
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

            // 404 - Not found
            {"AccountNotFoundException", typeof(AccountNotFoundException)},

            // 413 - Request Entity Too Large
            {"AccountQuantityException", typeof(AccountQuantityException)},
        };
    }
}
