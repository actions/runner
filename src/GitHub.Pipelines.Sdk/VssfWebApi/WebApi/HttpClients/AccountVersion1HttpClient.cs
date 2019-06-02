using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Account.Client
{
    [ResourceArea(AccountResourceIds.AreaId)]
    public class AccountVersion1HttpClient : VssHttpClientBase
    {
        static AccountVersion1HttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();

            s_translatedExceptions.Add("AccountExistsException", typeof(AccountExistsException));
            s_translatedExceptions.Add("AccountNotFoundException", typeof(AccountNotFoundException));
            s_translatedExceptions.Add("MaxNumberAccountsPerUserException", typeof(MaxNumberAccountsPerUserException));
            s_translatedExceptions.Add("MaxNumberAccountsException", typeof(MaxNumberAccountsException));
            s_translatedExceptions.Add("AccountPropertyException", typeof(AccountPropertyException));
            s_translatedExceptions.Add("IdentityNotFoundException", typeof(IdentityNotFoundException));
            s_translatedExceptions.Add("AccountUserNotFoundException", typeof(AccountUserNotFoundException));
        }

        internal AccountVersion1HttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        internal AccountVersion1HttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        internal AccountVersion1HttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        internal AccountVersion1HttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        internal AccountVersion1HttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            CurrentApiVersion = new ApiResourceVersion(c_apiVersion);
        }

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        public async Task<Account> CreateAccountAsync(
            string name,
            string organization,
            Guid creatorId,
            CultureInfo language = null,
            CultureInfo culture = null,
            TimeZoneInfo timeZone = null,
            IDictionary<string, object> properties = null,
            bool usePrecreated = false,
            List<KeyValuePair<Guid, Guid>> serviceDefinitions = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "CreateAccount"))
            {
                // NOTE: As this is a shipping, public API, please be careful when adding new parameters. Also, take care when adding an overload
                //       with new parameters that have defaults as that can cause compile-time binding issues.
                AccountCreateInfoInternal info = new AccountCreateInfoInternal(name, organization, creatorId, serviceDefinitions, language, culture, timeZone, properties);

                var query = AppendQueryString(
                    usePrecreated: usePrecreated);
                var account = await PostAsync<AccountCreateInfoInternal, Account>(
                    info,
                    AccountResourceIds.Account,
                    queryParameters: query,
                    version: CurrentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return account;
            }
        }

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.UserMapping.Client.UserMappingHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M112#AccountService_obsolescence for more details.")]
        public async Task<List<Account>> GetAccountsAsync(
            bool includeDisabledAccounts = false,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccounts"))
            {
                var query = AppendQueryString(
                    includeDisabledAccounts: includeDisabledAccounts,
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

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        internal async Task<List<Account>> GetAccountsByCreatorAsync(
            Guid creatorId,
            bool includeDisabledAccounts = false,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccountsByCreator"))
            {
                ArgumentUtility.CheckForEmptyGuid(creatorId, "creatorId");

                var query = AppendQueryString(
                    creatorId: creatorId,
                    includeDisabledAccounts: includeDisabledAccounts,
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

        public async Task<List<Account>> GetAccountsByOwnerAsync(
            Guid ownerId,
            bool includeDisabledAccounts = false,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccountsByOwner"))
            {
                ArgumentUtility.CheckForEmptyGuid(ownerId, "ownerId");

                var query = AppendQueryString(
                    ownerId: ownerId,
                    includeDisabledAccounts: includeDisabledAccounts,
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal Task<List<Account>> GetAccountsByMemberAsync(Guid memberId, bool includeOwner, bool includeDisabledAccounts, IEnumerable<string> propertyNameFilter, object userState)
        {
            // this overload has been preserved for binary compatibility, forwarding to a compatible overload that accepts CancellationToken
            return GetAccountsByMemberAsync(
                memberId,
                includeOwner,
                includeDisabledAccounts,
                propertyNameFilter,
                userState,
                default(CancellationToken));
        }

        public async Task<List<Account>> GetAccountsByMemberAsync(
            Guid memberId,
            bool includeOwner = true,
            bool includeDisabledAccounts = false,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccountsByMember"))
            {
                ArgumentUtility.CheckForEmptyGuid(memberId, "memberId");

                var query = AppendQueryString(
                    memberId: memberId,
                    includeOwner: includeOwner,
                    includeDisabledAccounts: includeDisabledAccounts,
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

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        public async Task<Account> GetAccountAsync(
            string accountId,
            IEnumerable<string> propertyNameFilter = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccount"))
            {
                var query = AppendQueryString(
                    propertyNames: propertyNameFilter);
                var account = await GetAsync<Account>(
                    AccountResourceIds.Account,
                    new { accountId = accountId },
                    version: CurrentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return account;
            }
        }

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        public Account GetAccount(string accountId, IEnumerable<string> propertyNameFilter = null, object userState = null)
        {
            try
            {
                return GetAccountAsync(
                    accountId,
                    propertyNameFilter,
                    userState,
                    default(CancellationToken)
                ).SyncResult();
            }
            catch (AccountNotFoundException)
            {
                return null;
            }
            catch (VssServiceResponseException e)
            {
                if (e.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        public async Task<IEnumerable<AccountRegion>> GetRegionsAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetRegions"))
            {
                var response = await GetAsync<IEnumerable<AccountRegion>>(
                    AccountResourceIds.AccountRegionLocationId,
                    userState: userState,
                    version: CurrentApiVersion,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return response;
            }
        }

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        public async Task<AccountNameAvailability> GetAccountNameAvailabilityAsync(
            string accountName,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "IsValidAccountName"))
            {
                ArgumentUtility.CheckStringForNullOrEmpty(accountName, "accountName");

                var response = await GetAsync<AccountNameAvailability>(
                    locationId: AccountResourceIds.AccountNameAvailabilityid,
                    routeValues: new { accountName },
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return response;
            }
        }

        [Obsolete("Please use appropriate method on Microsoft.VisualStudio.Services.Organization.Client.OrganizationHttpClient instead. See https://vsowiki.com/index.php?title=SDK_M113#AccountService_obsolescence for more details.")]
        public async Task<IDictionary<string, string>> GetAccountSettingsAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(AccountResourceIds.AccountServiceArea, "GetAccountSettingsAsync"))
            {
                var response = await GetAsync<IDictionary<string, string>>(
                    locationId: AccountResourceIds.AccountSettingsid,
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return response;
            }
        }

        /// <summary>
        /// Exceptions for account errors
        /// </summary>
        protected override IDictionary<String, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        protected List<KeyValuePair<string, string>> AppendQueryString(
            Guid? creatorId = null,
            Guid? ownerId = null,
            Guid? memberId = null,
            Guid? accountId = null,
            bool? includeOwner = false,
            bool? includeDisabledAccounts = false,
            bool? includeDeletedUsers = false,
            IEnumerable<string> propertyNames = null,
            bool? usePrecreated = false,
            string statusReason = null)
        {
            var collection = new List<KeyValuePair<string, string>>();

            if (creatorId != null) collection.Add(QueryParameters.CreatorId, creatorId.ToString());
            if (ownerId != null) collection.Add(QueryParameters.OwnerId, ownerId.ToString());
            if (memberId != null) collection.Add(QueryParameters.MemberId, memberId.ToString());
            if (accountId != null) collection.Add(QueryParameters.AccountId, accountId.ToString());
            if (statusReason != null) collection.Add(QueryParameters.StatusReason, statusReason);
            if (usePrecreated ?? false) collection.Add(QueryParameters.UsePrecreated, "true");
            if (includeOwner != null) collection.Add(QueryParameters.IncludeOwner, includeOwner.Value.ToString());
            if (includeDisabledAccounts ?? false) collection.Add(QueryParameters.IncludeDisabledAccounts, "true");
            if (includeDeletedUsers ?? false) collection.Add(QueryParameters.IncludeDeletedUsers, "true");
            if (propertyNames != null) collection.AddMultiple(QueryParameters.Properties, propertyNames);
            return collection;
        }

        private static Dictionary<string, Type> s_translatedExceptions;
        protected ApiResourceVersion CurrentApiVersion;

        private const double c_apiVersion = 1.0;
    }

    //we have to use this for now, because create is not strictly posting an Account object
    //TODO: Reconcile this with Account class
    //NOTE: This class and AccountPreferences are duplicated client and server...
    [DataContract(Name = "AccountCreateInfo")]
    internal class
        AccountCreateInfoInternal
    {
        internal AccountCreateInfoInternal(string name, string organization, Guid creatorId, CultureInfo language = null, CultureInfo culture = null, TimeZoneInfo timeZone = null, IDictionary<string, object> properties = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            Name = name;
            Organization = organization;
            Creator = creatorId;

            if (language != null || culture != null || timeZone != null)
            {
                Preferences = new AccountPreferencesInternal(language, culture, timeZone);
            }

            Properties = properties == null ? new PropertiesCollection() : new PropertiesCollection(properties);
        }

        internal AccountCreateInfoInternal(string name, string organization, Guid creatorId, List<KeyValuePair<Guid, Guid>> serviceDefinitions, CultureInfo language = null, CultureInfo culture = null, TimeZoneInfo timeZone = null, IDictionary<String, object> properties = null)
            : this(name, organization, creatorId, language, culture, timeZone, properties)
        {
            ServiceDefinitions = serviceDefinitions;
        }

        [DataMember(Name = "AccountName")]
        public string Name { get; private set; }

        [DataMember]
        public string Organization { get; private set; }

        [DataMember]
        public Guid Creator { get; private set; }

        [DataMember]
        public AccountPreferencesInternal Preferences { get; private set; }

        [DataMember]
        public PropertiesCollection Properties { get; private set; }

        [DataMember]
        public List<KeyValuePair<Guid, Guid>> ServiceDefinitions { get; set; }

        [DataContract(Name = "AccountPreferences")]
        internal class AccountPreferencesInternal
        {
            internal AccountPreferencesInternal(CultureInfo language, CultureInfo culture, TimeZoneInfo timeZone)
            {
                Language = language;
                Culture = culture;
                TimeZone = timeZone;
            }

            [DataMember]
            public CultureInfo Language { get; private set; }

            [DataMember]
            public CultureInfo Culture { get; private set; }

            [DataMember]
            public TimeZoneInfo TimeZone { get; private set; }
        }
    }
}