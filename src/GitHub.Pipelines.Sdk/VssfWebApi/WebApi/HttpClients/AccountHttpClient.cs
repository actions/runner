using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Account.Client
{
    [ResourceArea(AccountResourceIds.AreaId)]
    public class AccountHttpClient : AccountVersion1HttpClient
    {
        public AccountHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        public AccountHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        public AccountHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        public AccountHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        public AccountHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        public async Task<List<Account>> GetAccountsByOwnerAsync(
            Guid ownerId,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccountsByOwner"))
            {
                ArgumentUtility.CheckForEmptyGuid(ownerId, "ownerId");

                var query = AppendQueryString(
                    ownerId: ownerId,
                    propertyNames: propertyNameFilter);

                var accounts = await GetAsync<List<Account>>(
                    AccountResourceIds.Account,
                    version: CurrentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return accounts;
            }
        }

        public async Task<List<Account>> GetAccountsByMemberAsync(
            Guid memberId,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccountsByMember"))
            {
                ArgumentUtility.CheckForEmptyGuid(memberId, "memberId");

                var query = AppendQueryString(
                    memberId: memberId,
                    propertyNames: propertyNameFilter);

                var accounts = await GetAsync<List<Account>>(
                    AccountResourceIds.Account,
                    version: CurrentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return accounts;
            }
        }

        private const double c_apiVersion = 5.0;
    }
}