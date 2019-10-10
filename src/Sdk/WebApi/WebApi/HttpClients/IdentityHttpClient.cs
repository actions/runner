using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.DelegatedAuthorization;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Exceptions;
using GitHub.Services.Common.Internal;
using System.Linq;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.Identity.Client
{
    [ResourceArea(IdentityResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 100, failurePercentage:80, MaxConcurrentRequests = 110)]
    public class IdentityHttpClient : VssHttpClientBase
    {
        static IdentityHttpClient()
        {
            s_translatedExceptions = new Dictionary<string, Type>();

            // 400 (Bad Request)
            s_translatedExceptions.Add("IdentityDomainMismatchException", typeof(IdentityDomainMismatchException));
            s_translatedExceptions.Add("AddMemberCyclicMembershipException", typeof(AddMemberCyclicMembershipException));
            s_translatedExceptions.Add("IdentityPropertyRequiredException", typeof(IdentityPropertyRequiredException));
            s_translatedExceptions.Add("IdentityExpressionException", typeof(IdentityExpressionException));
            s_translatedExceptions.Add("InvalidDisplayNameException", typeof(InvalidDisplayNameException));
            s_translatedExceptions.Add("GroupNameNotRecognizedException", typeof(GroupNameNotRecognizedException));
            s_translatedExceptions.Add("IdentityMapReadOnlyException", typeof(IdentityMapReadOnlyException));
            s_translatedExceptions.Add("IdentityNotServiceIdentityException", typeof(IdentityNotServiceIdentityException));
            s_translatedExceptions.Add("InvalidServiceIdentityNameException", typeof(InvalidServiceIdentityNameException));
            s_translatedExceptions.Add("IllegalIdentityException", typeof(IllegalIdentityException));
            s_translatedExceptions.Add("MissingRequiredParameterException", typeof(MissingRequiredParameterException));
            s_translatedExceptions.Add("IncompatibleScopeException", typeof(IncompatibleScopeException));

            // 403 (Forbidden)
            s_translatedExceptions.Add("RemoveAccountOwnerFromAdminGroupException", typeof(RemoveAccountOwnerFromAdminGroupException));
            s_translatedExceptions.Add("RemoveSelfFromAdminGroupException", typeof(RemoveSelfFromAdminGroupException));
            s_translatedExceptions.Add("AddGroupMemberIllegalMemberException", typeof(AddGroupMemberIllegalMemberException));
            s_translatedExceptions.Add("AddGroupMemberIllegalWindowsIdentityException", typeof(AddGroupMemberIllegalWindowsIdentityException));
            s_translatedExceptions.Add("AddGroupMemberIllegalInternetIdentityException", typeof(AddGroupMemberIllegalInternetIdentityException));
            s_translatedExceptions.Add("RemoveSpecialGroupException", typeof(RemoveSpecialGroupException));
            s_translatedExceptions.Add("NotApplicationGroupException", typeof(NotApplicationGroupException));
            s_translatedExceptions.Add("ModifyEveryoneGroupException", typeof(ModifyEveryoneGroupException));
            s_translatedExceptions.Add("NotASecurityGroupException", typeof(NotASecurityGroupException));
            s_translatedExceptions.Add("RemoveMemberServiceAccountException", typeof(RemoveMemberServiceAccountException));
            s_translatedExceptions.Add("AccountPreferencesAlreadyExistException", typeof(AccountPreferencesAlreadyExistException));

            // 404 (NotFound)
            s_translatedExceptions.Add("RemoveGroupMemberNotMemberException", typeof(RemoveGroupMemberNotMemberException));
            s_translatedExceptions.Add("RemoveNonexistentGroupException", typeof(RemoveNonexistentGroupException));
            s_translatedExceptions.Add("FindGroupSidDoesNotExistException", typeof(FindGroupSidDoesNotExistException));
            s_translatedExceptions.Add("GroupScopeDoesNotExistException", typeof(GroupScopeDoesNotExistException));
            s_translatedExceptions.Add("IdentityNotFoundException", typeof(IdentityNotFoundException));

            // 409 (Conflict)
            s_translatedExceptions.Add("GroupCreationException", typeof(GroupCreationException));
            s_translatedExceptions.Add("GroupScopeCreationException", typeof(GroupScopeCreationException));
            s_translatedExceptions.Add("AddMemberIdentityAlreadyMemberException", typeof(AddMemberIdentityAlreadyMemberException));
            s_translatedExceptions.Add("GroupRenameException", typeof(GroupRenameException));
            s_translatedExceptions.Add("IdentityAlreadyExistsException", typeof(IdentityAlreadyExistsException));
            s_translatedExceptions.Add("IdentityAccountNameAlreadyInUseException", typeof(IdentityAccountNameAlreadyInUseException));
            s_translatedExceptions.Add("IdentityAliasAlreadyInUseException", typeof(IdentityAliasAlreadyInUseException));
            s_translatedExceptions.Add("AddProjectGroupProjectMismatchException", typeof(AddProjectGroupProjectMismatchException));

            // 500 (InternalServerError)
            s_translatedExceptions.Add("IdentitySyncException", typeof(IdentitySyncException));

            // 503 (ServiceUnavailable)
            s_translatedExceptions.Add("IdentityProviderUnavailableException", typeof(IdentityProviderUnavailableException));

            s_currentApiVersion = new ApiResourceVersion(1.0);
        }

        public IdentityHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public IdentityHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public IdentityHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public IdentityHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public IdentityHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #region Operations on Identities Controller
        #region ReadIdentities overloads
        /// <summary>
        /// Reads all identities
        /// </summary>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public Task<IdentitiesCollection> ReadIdentitiesAsync(
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queryParams = new List<KeyValuePair<string, string>>();

            return ReadIdentitiesAsyncInternal(queryParams, queryMembership, propertyNameFilters, includeRestrictedVisibility, requestHeadersContext: null, userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns identities matching the passed in descriptors
        /// </summary>
        /// <param name="descriptors">List of IdentityDescriptors to query for.</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<IdentityDescriptor> descriptors,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReadIdentitiesAsync(descriptors, requestHeadersContext: null, queryMembership: queryMembership,
                propertyNameFilters: propertyNameFilters, includeRestrictedVisibility: includeRestrictedVisibility,
                userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns identities matching the passed in descriptors
        /// </summary>
        /// <param name="descriptors">List of IdentityDescriptors to query for.</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<IdentityDescriptor> descriptors,
            RequestHeadersContext requestHeadersContext,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(descriptors, "descriptors");

            if (descriptors.Count > maxDescriptors)
            {
                return ReadIdentitiesBatchAsyncInternal(
                    descriptors,
                    queryMembership,
                    propertyNameFilters,
                    includeRestrictedVisibility,
                    requestHeadersContext,
                    userState, cancellationToken);
            }
            else
            {
                var pages = new List<KeyValuePair<string, string>>();

                pages.AddMultiple(QueryParameters.Descriptors, descriptors, SerializeDescriptor);

                return ReadIdentitiesAsyncInternal(pages, queryMembership, propertyNameFilters, includeRestrictedVisibility, requestHeadersContext, userState, cancellationToken);
            }
        }

        /// <summary>
        /// Returns identities matching the passed in subject descriptors
        /// </summary>
        /// <param name="socialDescriptors">List of SocialDescriptors to query for.</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<SocialDescriptor> socialDescriptors,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReadIdentitiesAsync(socialDescriptors, requestHeadersContext: null, queryMembership: queryMembership,
                propertyNameFilters: propertyNameFilters, includeRestrictedVisibility: includeRestrictedVisibility,
                userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns identities matching the passed in descriptors
        /// </summary>
        /// <param name="subjectDescriptors">List of SubjectDescriptors to query for.</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        internal virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<SocialDescriptor> socialDescriptors,
            RequestHeadersContext requestHeadersContext,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(socialDescriptors, nameof(socialDescriptors));

            if (socialDescriptors.Count > maxDescriptors)
            {
                return ReadIdentitiesBatchAsyncInternal(
                    socialDescriptors,
                    queryMembership,
                    propertyNameFilters,
                    includeRestrictedVisibility,
                    requestHeadersContext,
                    userState, cancellationToken);
            }
            else
            {
                var pages = new List<KeyValuePair<string, string>>();

                pages.AddMultiple(QueryParameters.SocialDescriptors, socialDescriptors.Select(descriptor => descriptor.ToString()).ToList());

                return ReadIdentitiesAsyncInternal(pages, queryMembership, propertyNameFilters, includeRestrictedVisibility, requestHeadersContext, userState, cancellationToken);
            }
        }

        /// <summary>
        /// Returns identities matching the passed in subject descriptors
        /// </summary>
        /// <param name="subjectDescriptors">List of SubjectDescriptors to query for.</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<SubjectDescriptor> subjectDescriptors,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReadIdentitiesAsync(subjectDescriptors, requestHeadersContext: null, queryMembership: queryMembership,
                propertyNameFilters: propertyNameFilters, includeRestrictedVisibility: includeRestrictedVisibility,
                userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns identities matching the passed in descriptors
        /// </summary>
        /// <param name="subjectDescriptors">List of SubjectDescriptors to query for.</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        internal virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<SubjectDescriptor> subjectDescriptors,
            RequestHeadersContext requestHeadersContext,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(subjectDescriptors, nameof(subjectDescriptors));

            if (subjectDescriptors.Count > maxDescriptors)
            {
                return ReadIdentitiesBatchAsyncInternal(
                    subjectDescriptors,
                    queryMembership,
                    propertyNameFilters,
                    includeRestrictedVisibility,
                    requestHeadersContext,
                    userState, cancellationToken);
            }
            else
            {
                var pages = new List<KeyValuePair<string, string>>();

                pages.AddMultiple(QueryParameters.SubjectDescriptors, subjectDescriptors.Select(descriptor => descriptor.ToString()).ToList());

                return ReadIdentitiesAsyncInternal(pages, queryMembership, propertyNameFilters, includeRestrictedVisibility, requestHeadersContext, userState, cancellationToken);
            }
        }

        /// <summary>
        /// Returns identities matching the passed in identifiers
        /// </summary>
        /// <param name="identityIds">Guids representing unique identifiers for the identities</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<Guid> identityIds,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReadIdentitiesAsync(identityIds, requestHeadersContext: null, queryMembership: queryMembership,
                propertyNameFilters: propertyNameFilters, includeRestrictedVisibility: includeRestrictedVisibility,
                userState: userState, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns identities matching the passed in identifiers
        /// </summary>
        /// <param name="identityIds">Guids representing unique identifiers for the identities</param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        internal virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IList<Guid> identityIds,
            RequestHeadersContext requestHeadersContext,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            bool includeRestrictedVisibility = false,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(identityIds, "identityIds");

            if (identityIds.Count > maxIds)
            {
                return ReadIdentitiesBatchAsyncInternal(
                    identityIds,
                    queryMembership,
                    propertyNameFilters,
                    includeRestrictedVisibility,
                    userState, 
                    requestHeadersContext,
                    cancellationToken);
            }
            else
            {
                var pages = new List<KeyValuePair<string, string>>();

                pages.AddMultiple(QueryParameters.IdentityIds, identityIds, (id) => id.ToString("N"));

                return ReadIdentitiesAsyncInternal(pages, queryMembership, propertyNameFilters, includeRestrictedVisibility, requestHeadersContext, userState, cancellationToken);
            }
        }

        public Task<IdentitiesCollection> ReadIdentitiesAsync(
            IdentitySearchFilter searchFilter,
            string filterValue,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReadIdentitiesAsync(searchFilter, filterValue, ReadIdentitiesOptions.None, queryMembership, propertyNameFilters, userState, cancellationToken);
        }

        /// <summary>
        /// Returns identities matching the requested search factor and value
        /// </summary>
        /// <param name="searchFilter"></param>
        /// <param name="filterValue"></param>
        /// <param name="queryMembership">Instructs the server whether to query for membership information.</param>
        /// <param name="propertyNameFilters">Instructs the server which extended properties to query for.</param>
        /// <param name="userState">Additional client state passed by caller.</param>
        /// <returns>A Task which when complete, contains the list of identities.</returns>
        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            IdentitySearchFilter searchFilter,
            string filterValue,
            ReadIdentitiesOptions options,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(filterValue, "filterValue");

            List<KeyValuePair<string, string>> searchQuery = new List<KeyValuePair<string, string>>();

            searchQuery.Add(QueryParameters.SearchFilter, searchFilter.ToString());
            searchQuery.Add(QueryParameters.FilterValue, filterValue);
            searchQuery.Add(QueryParameters.ReadIdentitiesOptions, options.ToString());

            return ReadIdentitiesAsyncInternal(searchQuery, queryMembership, propertyNameFilters, includeRestrictedVisibility: false, requestHeadersContext: null, userState: userState, cancellationToken: cancellationToken);
        }

        public virtual Task<IdentitiesCollection> ReadIdentitiesAsync(
            Guid scopeId,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = new List<KeyValuePair<string, string>>();
            query.Add(QueryParameters.ScopeId, scopeId.ToString("N"));

            return ReadIdentitiesAsyncInternal(query, queryMembership, propertyNameFilters, includeRestrictedVisibility: false, requestHeadersContext: null, userState: userState, cancellationToken: cancellationToken);
        }
        #endregion

        #region ReadIdentity overloads
        public Task<Identity> ReadIdentityAsync(
            string identityPuid,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(identityPuid, "identityPuid");

            return ReadIdentityAsyncInternal(
                identityPuid,
                queryMembership,
                propertyNameFilters,
                userState, cancellationToken);
        }

        public Task<Identity> ReadIdentityAsync(
            Guid identityId,
            QueryMembership queryMembership = QueryMembership.None,
            IEnumerable<string> propertyNameFilters = null,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(identityId, "identityId");

            return ReadIdentityAsyncInternal(
                identityId.ToString("D"),
                queryMembership,
                propertyNameFilters,
                userState, cancellationToken);
        }
        #endregion

        public async Task<IEnumerable<IdentityUpdateData>> UpdateIdentitiesAsync(IList<Identity> identities, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "UpdateIdentities"))
            {
                ArgumentUtility.CheckEnumerableForNullOrEmpty(identities, "identities");

                IdentitiesCollection collection = new IdentitiesCollection(identities);
                HttpContent content = new ObjectContent<VssJsonCollectionWrapper<IdentitiesCollection>>(new VssJsonCollectionWrapper<IdentitiesCollection>(collection), base.Formatter);

                return await SendAsync<IEnumerable<IdentityUpdateData>>(HttpMethod.Put, IdentityResourceIds.Identity, version: s_currentApiVersion, content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> UpdateIdentityAsync(Identity identity, object userState, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "UpdateIdentity"))
            {
                ArgumentUtility.CheckForNull(identity, "identity");

                HttpContent content = new ObjectContent<Identity>(identity, base.Formatter);
                return await SendAsync(HttpMethod.Put, IdentityResourceIds.Identity, new { identityId = identity.Id }, s_currentApiVersion, content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> SwapIdentityAsync(Guid id1, Guid id2, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "SwapIdentity"))
            {
                ArgumentUtility.CheckForEmptyGuid(id1, "id1");
                ArgumentUtility.CheckForEmptyGuid(id2, "id2");

                HttpContent content = new ObjectContent(typeof(SwapIdentityInfo), new SwapIdentityInfo(id1, id2), this.Formatter);

                return await SendAsync(HttpMethod.Post, IdentityResourceIds.SwapLocationId, version: s_currentApiVersion, content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        
        //REST USAGE NON-STANDARD: Get operations on the same endpoint should return the same resources. This is a different 
        //resource.
        public async Task<ChangedIdentities> GetIdentityChangesAsync(int identitySequenceId, int groupSequenceId, Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            int unspecifiedSequenceId = -1;
            return await this.GetIdentityChangesAsync(identitySequenceId, groupSequenceId, unspecifiedSequenceId, scopeId, userState, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ChangedIdentities> GetIdentityChangesAsync(int identitySequenceId, int groupSequenceId, int organizationIdentitySequenceId, Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.GetIdentityChangesAsync(identitySequenceId, groupSequenceId, organizationIdentitySequenceId, 0, scopeId, userState, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ChangedIdentities> GetIdentityChangesAsync(int identitySequenceId, int groupSequenceId, int organizationIdentitySequenceId, int pageSize, Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetIdentityChanges"))
            {
                List<KeyValuePair<string, string>> query = new List<KeyValuePair<string, string>>();
                query.Add(QueryParameters.IdentitySequenceId, identitySequenceId.ToString());
                query.Add(QueryParameters.GroupSequenceId, groupSequenceId.ToString());
                query.Add(QueryParameters.OrgIdentitySequenceId, organizationIdentitySequenceId.ToString());
                query.Add(QueryParameters.PageSize, pageSize.ToString());
                query.Add(QueryParameters.ScopeId, scopeId.ToString("N"));

                return await SendAsync<ChangedIdentities>(HttpMethod.Get, IdentityResourceIds.Identity, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IList<Guid>> GetUserIdentityIdsByDomainIdAsync(
            Guid domainId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, nameof(GetUserIdentityIdsByDomainIdAsync)))
            {
                ArgumentUtility.CheckForEmptyGuid(domainId, nameof(domainId));
                var query = new List<KeyValuePair<string, string>>();
                query.Add(QueryParameters.DomainId, domainId.ToString("N"));
                return
                    await
                    SendAsync<IList<Guid>>(
                        method: HttpMethod.Get,
                        locationId: IdentityResourceIds.Identity,
                        version: s_currentApiVersion,
                        queryParameters: query,
                        userState: userState,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Operations on IdentitySelf Controller

        public async Task<IdentitySelf> GetIdentitySelfAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetIdentitySelf"))
            {
                return await SendAsync<IdentitySelf>(HttpMethod.Get, IdentityResourceIds.IdentitySelf, version: s_currentApiVersion, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Operations on IdentityTenant Controller

        public async Task<TenantInfo> GetTenant(string tenantId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetTenant"))
            {
                //NOTE [RR]: Having to re-create ApiResourceLocation here since /_apis is currently not
                //Anonymous and using underlying SendAsync<> overloads throws a ObjectNullRefernceException
                //when a null credential, indicating anonymous request, is 
                var resourceLocation = new ApiResourceLocation
                {
                    Id = IdentityResourceIds.IdentityTenant,
                    ResourceName = IdentityResourceIds.IdentityTenantResource,
                    RouteTemplate = "_apis/identities/tenant/{tenantId}",
                    ResourceVersion = 1,
                    MinVersion = new Version(1, 0),
                    MaxVersion = new Version(2, 0),
                    ReleasedVersion = new Version(0, 0)
                };

                using (var requestMessage = CreateRequestMessage(HttpMethod.Get, resourceLocation, new { tenantId = tenantId }, version: s_currentApiVersion))
                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(requestMessage, cancellationToken: cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsAsync<TenantInfo>(new[] { this.Formatter }, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region Operations on service identities controller

        public async Task<Identity> CreateFrameworkIdentityAsync(FrameworkIdentityType identityType, string role, string identifier, string displayName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "CreateServiceIdentity"))
            {
                if (identityType == FrameworkIdentityType.None)
                {
                    throw new ArgumentException(CommonResources.EmptyStringNotAllowed(), "identityType");
                }

                ArgumentUtility.CheckStringForNullOrEmpty(displayName, "role");
                ArgumentUtility.CheckStringForNullOrEmpty(displayName, "identifier");
                ArgumentUtility.CheckStringForNullOrEmpty(displayName, "displayName");

                HttpContent content = new ObjectContent(
                    typeof(FrameworkIdentityInfo),
                    new FrameworkIdentityInfo
                    {
                        IdentityType = identityType,
                        Role = role,
                        Identifier = identifier,
                        DisplayName = displayName
                    },
                    this.Formatter);

                return await SendAsync<Identity>(HttpMethod.Put, IdentityResourceIds.FrameworkIdentity, version: s_currentApiVersion, content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Operations on Groups Controller
        public virtual async Task<IdentitiesCollection> ListGroupsAsync(Guid[] scopeIds = null, bool recurse = false, bool deleted = false, IEnumerable<string> propertyNameFilters = null, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ListGroups"))
            {
                List<KeyValuePair<string, string>> query = null;

                if (scopeIds != null || recurse != false || propertyNameFilters != null)
                {
                    query = new List<KeyValuePair<string, string>>();

                    if (scopeIds != null)
                    {
                        query.AddMultiple(QueryParameters.ScopeIds, scopeIds, (val) => val.ToString("N"));
                    }

                    if (recurse != false)
                    {
                        query.Add(QueryParameters.Recurse, "true");
                    }

                    if (deleted != false)
                    {
                        query.Add(QueryParameters.Deleted, "true");
                    }

                    if (propertyNameFilters != null)
                    {
                        query.AddMultiple(QueryParameters.Properties, propertyNameFilters);
                    }
                }

                return await SendAsync<IdentitiesCollection>(HttpMethod.Get, IdentityResourceIds.Group, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<HttpResponseMessage> DeleteGroupAsync(IdentityDescriptor descriptor, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteGroupAsyncInternal(SerializeDescriptor(descriptor), userState, cancellationToken: cancellationToken);
        }

        public Task<HttpResponseMessage> DeleteGroupAsync(Guid groupId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteGroupAsyncInternal(groupId.ToString(), userState, cancellationToken);
        }

        public async Task<IdentitiesCollection> CreateGroupsAsync(Guid scopeId, IList<Identity> groups, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "CreateGroup"))
            {
                ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");
                ArgumentUtility.CheckEnumerableForNullOrEmpty(groups, "groups");

                HttpContent content = new ObjectContent<CreateGroupsInfo>(new CreateGroupsInfo(scopeId, groups), this.Formatter);

                return await SendAsync<IdentitiesCollection>(HttpMethod.Post, IdentityResourceIds.Group, version: s_currentApiVersion, content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion

        #region Operations on Scopes Controller
        public async Task<IdentityScope> GetScopeAsync(string scopeName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetScope"))
            {
                ArgumentUtility.CheckStringForNullOrEmpty(scopeName, "scopeName");

                List<KeyValuePair<string, string>> query = new List<KeyValuePair<string, string>>();
                query.Add(QueryParameters.ScopeName, scopeName);

                return await SendAsync<IdentityScope>(HttpMethod.Get, IdentityResourceIds.Scope, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IdentityScope> GetScopeAsync(Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetScopeById"))
            {
                ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");

                return await SendAsync<IdentityScope>(HttpMethod.Get, IdentityResourceIds.Scope, new { scopeId = scopeId }, version: s_currentApiVersion, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IdentityScope> CreateScopeAsync(Guid scopeId, Guid parentScopeId, GroupScopeType scopeType, string scopeName, string adminGroupName, string adminGroupDescription, Guid creatorId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "CreateScope"))
            {
                ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");

                //REST USAGE NON-STANDARD: A POST to create a resource should be a reprensentation of the resource being created, in this case an IdentityScope. However, 
                //the create operation takes parameters not present in the new resource: specifically the adminGroupName and adminGroupDescription. We either need
                //to set these in a different way -- on the correct resource -- or include them as part of IdentityScope.

                // Constructor Validates params
                CreateScopeInfo info = new CreateScopeInfo(parentScopeId, scopeType, scopeName, adminGroupName, adminGroupDescription, creatorId);

                HttpContent content = new ObjectContent<CreateScopeInfo>(info, this.Formatter);

                return await SendAsync<IdentityScope>(HttpMethod.Put, IdentityResourceIds.Scope, new { scopeId = scopeId }, version: s_currentApiVersion, content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> RenameScopeAsync(Guid scopeId, string newName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "RenameScope"))
            {
                ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");
                ArgumentUtility.CheckStringForNullOrEmpty(newName, "newName");

                IdentityScope rename = new IdentityScope(scopeId, newName);
                HttpContent content = new ObjectContent<IdentityScope>(rename, this.Formatter);

                return await SendAsync(new HttpMethod("PATCH"), IdentityResourceIds.Scope, new { scopeId = scopeId }, version: s_currentApiVersion, content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            // code for version 2 of the API - lets switch to this after the new api version has been in for a sprint
            //ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");
            //ArgumentUtility.CheckStringForNullOrEmpty(newName, "newName");
            //return await UpdateScopeAsync(scopeId, nameof(IdentityScope.Name), newName, userState, cancellationToken);
        }

        public async Task<HttpResponseMessage> DeleteScopeAsync(Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "DeleteScope"))
            {
                ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");

                return await SendAsync(HttpMethod.Delete, IdentityResourceIds.Scope, new { scopeId = scopeId }, version: s_currentApiVersion, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> RestoreGroupScopeAsync(Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(scopeId, "scopeId");
            return await UpdateScopeAsync(scopeId, nameof(IdentityScope.IsActive), true, userState, cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> UpdateScopeAsync(Guid scopeId, String property, object value, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using(new OperationScope(IdentityResourceIds.AreaName, "UpdateScope"))
            {
                JsonPatchDocument patchDocument = new JsonPatchDocument{
                    new JsonPatchOperation
                    {
                        Operation = WebApi.Patch.Operation.Replace,
                        Path = "/" + property,
                        Value = value
                    }
                };

                HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

                return await SendAsync<HttpResponseMessage>(new HttpMethod("PATCH"), IdentityResourceIds.Scope, new { scopeId = scopeId }, version: new ApiResourceVersion(5.0, 2), content: content, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion

        #region Operations on Members\MembersOf Controllers\
        //These methods have analogs on the Members\MemberOf controllers but are unused...
        //Task<IdentityDescriptorCollection> ReadMembershipsAsync(String memberId, QueryMembership queryMembership = QueryMembership.Direct)

        //This one called by IsMember, but not exposed directly
        //Task<IdentityDescriptorCollection> ReadMembershipsAsync(String memberId, String containerId, QueryMembership queryMembership = QueryMembership.Direct)

        //Task<IdentityDescriptorCollection> ReadMembersAsync(String containerId, QueryMembership queryMembership = QueryMembership.Direct)

        //Task<IdentityDescriptor> ReadMemberAsync(String containerId, String memberId, QueryMembership queryMembership = QueryMembership.Direct)

        public Task<bool> AddMemberToGroupAsync(IdentityDescriptor containerId, Guid memberId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AddMemberToGroupAsyncInternal(new { containerId = SerializeDescriptor(containerId), memberId = memberId }, new List<KeyValuePair<string, string>>(), userState, cancellationToken);
        }

        public Task<bool> AddMemberToGroupAsync(IdentityDescriptor containerId, IdentityDescriptor memberId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            //REST USAGE NON-STANDARD: This should not be a query param, as this ends up being a PUT, which should address the resource directly
            // (and also see the internal method on non-standard use of PUT). But the memberId may contain a colon, which will cause it to 
            //be rejected by ASP.NET as dangerous (even if escaped) so doing this as a workaround.
            List<KeyValuePair<string, string>> query = new List<KeyValuePair<string, string>>();
            query.Add(QueryParameters.MemberId, SerializeDescriptor(memberId));

            return AddMemberToGroupAsyncInternal(new { containerId = SerializeDescriptor(containerId) }, query, userState, cancellationToken);
        }

        public async Task<bool> RemoveMemberFromGroupAsync(IdentityDescriptor containerId, IdentityDescriptor memberId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "RemoveMemberFromGroup"))
            {
                //REST USAGE NON-STANDARD: This should not be a query param, as this DELETE which should address the resource directly
                //but the memberId may contain a colon, which will cause it to be rejected by ASP.NET as dangerous (even if escaped) so doing
                //this as a workaround.
                List<KeyValuePair<string, string>> query = new List<KeyValuePair<string, string>>();
                query.Add(QueryParameters.MemberId, SerializeDescriptor(memberId));

                return await SendAsync<bool>(HttpMethod.Delete, IdentityResourceIds.Member, new { containerId = SerializeDescriptor(containerId) }, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> IsMember(IdentityDescriptor containerId, IdentityDescriptor memberId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "IsMember"))
            {
                List<KeyValuePair<string, string>> query = new List<KeyValuePair<string, string>>();
                query.Add(QueryParameters.QueryMembership, QueryMembership.Expanded.ToString());

                //Consider: Can this actually return null? This is how IdentityHttpComponent works...
                IdentityDescriptor result = await SendAsync<IdentityDescriptor>(HttpMethod.Get, IdentityResourceIds.MemberOf,
                    new { memberId = SerializeDescriptor(memberId), containerId = SerializeDescriptor(containerId) },
                    version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);

                return result != null;
            }
        }
        #endregion

        #region Operations on IdentitySnapshot controller
        public async Task<IdentitySnapshot> GetIdentitySnapshotAsync(Guid scopeId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetIdentitySnapshot"))
            {
                return await SendAsync<IdentitySnapshot>(HttpMethod.Get, IdentityResourceIds.IdentitySnapshot, version: s_currentApiVersion, routeValues: new { scopeId = scopeId }, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Operations on SignoutToken controller
        public async Task<AccessTokenResult> GetSignoutToken(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetSignoutToken"))
            {
                return await SendAsync<AccessTokenResult>(
                    HttpMethod.Get,
                    IdentityResourceIds.SignoutToken,
                    version: s_currentApiVersion,
                    routeValues: new object { },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion

        #region Operations on SignedInToken controller
        public async Task<AccessTokenResult> GetSignedInToken(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetSignedInToken"))
            {
                return await SendAsync<AccessTokenResult>(
                    HttpMethod.Get,
                    IdentityResourceIds.SignedInToken,
                    version: s_currentApiVersion,
                    routeValues: new object { },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion

        #region Operations on IdentitySequenceId Controller
        public async Task<int> GetMaxSequenceIdAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "GetMaxSequenceId"))
            {
                return await SendAsync<int>(HttpMethod.Get, IdentityResourceIds.IdentityMaxSequenceId, version: s_currentApiVersion, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion Operations on IdentitySequenceId Controller

        #region Operations on Claims Controller
        public async Task<Identity> CreateOrBindIdentity(Identity sourceIdentity, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "CreateOrBindWithClaims"))
            {
                ArgumentUtility.CheckForNull(sourceIdentity, nameof(sourceIdentity));
                ArgumentUtility.CheckForNull(sourceIdentity.Descriptor, nameof(sourceIdentity.Descriptor));

                HttpContent content = new ObjectContent<Identity>(sourceIdentity, this.Formatter);

                return await SendAsync<Identity>(HttpMethod.Put,
                    IdentityResourceIds.Claims,
                    version: s_currentApiVersion,
                    userState: userState,
                    content: content,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion Operations on Claims Controller

        #region Operations on IdentityDescriptor Controller
        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isMasterId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task<IdentityDescriptor> GetDescriptorByIdAsync(
            Guid id,
            bool? isMasterId = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            object routeValues = new { id = id };

            var queryParams = new List<KeyValuePair<string, string>>();
            if (isMasterId != null)
            {
                queryParams.Add("isMasterId", isMasterId.Value.ToString());
            }

            return await SendAsync<IdentityDescriptor>(
                HttpMethod.Get,
                IdentityResourceIds.DescriptorsResourceLocationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("3.2-preview.1"),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken:
                cancellationToken).ConfigureAwait(false);
        }
        #endregion Operations on IdentityDescriptor Controller

        #region Private Helpers
        private async Task<IdentitiesCollection> ReadIdentitiesAsyncInternal(List<KeyValuePair<string, string>> searchQuery, QueryMembership queryMembership, IEnumerable<string> propertyNameFilters, bool includeRestrictedVisibility, RequestHeadersContext  requestHeadersContext, object userState, CancellationToken cancellationToken)
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ReadIdentities"))
            {
                AppendQueryString(searchQuery, queryMembership, propertyNameFilters, includeRestrictedVisibility);
                var additionalHeaders = RequestHeadersContext.HeadersUtils.PopulateRequestHeaders(requestHeadersContext);

                return await SendAsync<IdentitiesCollection>(HttpMethod.Get, additionalHeaders, IdentityResourceIds.Identity, version: s_currentApiVersion, queryParameters: searchQuery, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IdentitiesCollection> ReadIdentitiesBatchAsyncInternal(
            IList<SocialDescriptor> socialDescriptors,
            QueryMembership queryMembership,
            IEnumerable<string> propertyNameFilters,
            bool includeRestrictedVisibility,
            RequestHeadersContext requestHeadersContext,
            object userState, CancellationToken cancellationToken)
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ReadIdentitiesBatch"))
            {
                IdentityBatchInfo info = new IdentityBatchInfo(socialDescriptors, queryMembership, propertyNameFilters, includeRestrictedVisibility);

                HttpContent content = new ObjectContent<IdentityBatchInfo>(info, base.Formatter);

                var queryParams = new List<KeyValuePair<string, string>>()
                {
                    {IdentityBatchTelemetryConstants.QueryMembershipHint, queryMembership.ToString()},
                    {IdentityBatchTelemetryConstants.FlavorHint, IdentityBatchTelemetryConstants.BySocialDescriptorFlavor },
                    {IdentityBatchTelemetryConstants.CountHint, (socialDescriptors?.Count ?? 0).ToString() },
                };

                var additionalHeaders = RequestHeadersContext.HeadersUtils.PopulateRequestHeaders(requestHeadersContext);

                return await SendAsync<IdentitiesCollection>(
                    HttpMethod.Post,
                    additionalHeaders,
                    IdentityResourceIds.IdentityBatch,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    queryParameters: queryParams,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IdentitiesCollection> ReadIdentitiesBatchAsyncInternal(
            IList<SubjectDescriptor> subjectDescriptors,
            QueryMembership queryMembership,
            IEnumerable<string> propertyNameFilters,
            bool includeRestrictedVisibility,
            RequestHeadersContext requestHeadersContext,
            object userState, CancellationToken cancellationToken)
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ReadIdentitiesBatch"))
            {
                IdentityBatchInfo info = new IdentityBatchInfo(subjectDescriptors, queryMembership, propertyNameFilters, includeRestrictedVisibility);

                HttpContent content = new ObjectContent<IdentityBatchInfo>(info, base.Formatter);

                var queryParams = new List<KeyValuePair<string, string>>()
                {
                    {IdentityBatchTelemetryConstants.QueryMembershipHint, queryMembership.ToString()},
                    {IdentityBatchTelemetryConstants.FlavorHint, IdentityBatchTelemetryConstants.BySubjectDescriptorFlavor },
                    {IdentityBatchTelemetryConstants.CountHint, (subjectDescriptors?.Count ?? 0).ToString() },
                };

                var additionalHeaders = RequestHeadersContext.HeadersUtils.PopulateRequestHeaders(requestHeadersContext);

                return await SendAsync<IdentitiesCollection>(
                    HttpMethod.Post,
                    additionalHeaders,
                    IdentityResourceIds.IdentityBatch,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    queryParameters: queryParams,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IdentitiesCollection> ReadIdentitiesBatchAsyncInternal(
            IList<IdentityDescriptor> descriptors,
            QueryMembership queryMembership,
            IEnumerable<string> propertyNameFilters,
            bool includeRestrictedVisibility,
            RequestHeadersContext requestHeadersContext,
            object userState, CancellationToken cancellationToken)
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ReadIdentitiesBatch"))
            {
                IdentityBatchInfo info = new IdentityBatchInfo(descriptors, queryMembership, propertyNameFilters, includeRestrictedVisibility);

                HttpContent content = new ObjectContent<IdentityBatchInfo>(info, base.Formatter);

                var queryParams = new List<KeyValuePair<string, string>>()
                {
                    {IdentityBatchTelemetryConstants.QueryMembershipHint, queryMembership.ToString()},
                    {IdentityBatchTelemetryConstants.FlavorHint, IdentityBatchTelemetryConstants.ByDescriptorFlavor },
                    {IdentityBatchTelemetryConstants.CountHint, (descriptors?.Count ?? 0).ToString() },
                };

                var additionalHeaders = RequestHeadersContext.HeadersUtils.PopulateRequestHeaders(requestHeadersContext);

                return await SendAsync<IdentitiesCollection>(
                    HttpMethod.Post,
                    additionalHeaders,
                    IdentityResourceIds.IdentityBatch,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    queryParameters: queryParams,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IdentitiesCollection> ReadIdentitiesBatchAsyncInternal(
            IList<Guid> identityIds,
            QueryMembership queryMembership,
            IEnumerable<string> propertyNameFilters,
            bool includeRestrictedVisibility,
            object userState,
            RequestHeadersContext requestHeadersContext,
            CancellationToken cancellationToken)
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ReadIdentitiesBatch"))
            {
                IdentityBatchInfo info = new IdentityBatchInfo(identityIds, queryMembership, propertyNameFilters, includeRestrictedVisibility);

                HttpContent content = new ObjectContent<IdentityBatchInfo>(info, base.Formatter);

                var queryParams = new List<KeyValuePair<string, string>>
                {
                    {IdentityBatchTelemetryConstants.QueryMembershipHint, queryMembership.ToString()},
                    {IdentityBatchTelemetryConstants.FlavorHint, IdentityBatchTelemetryConstants.ByIdFlavor },
                    {IdentityBatchTelemetryConstants.CountHint, (identityIds?.Count ?? 0).ToString() },
                };

                var additionalHeaders = RequestHeadersContext.HeadersUtils.PopulateRequestHeaders(requestHeadersContext);

                return await SendAsync<IdentitiesCollection>(
                    HttpMethod.Post,
                    additionalHeaders,
                    IdentityResourceIds.IdentityBatch,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    queryParameters: queryParams,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        //Separate endpoint for identity and identities
        private async Task<Identity> ReadIdentityAsyncInternal(
            string identityId,
            QueryMembership queryMembership,
            IEnumerable<string> propertyNameFilters,
            object userState, CancellationToken cancellationToken)
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "ReadIdentity"))
            {
                var query = new List<KeyValuePair<string, string>>();
                AppendQueryString(query, queryMembership, propertyNameFilters, false);

                return await SendAsync<Identity>(HttpMethod.Get, IdentityResourceIds.Identity, new { identityId = identityId }, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<HttpResponseMessage> DeleteGroupAsyncInternal(string groupId, object userState, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "DeleteGroup"))
            {
                return await SendAsync(HttpMethod.Delete, IdentityResourceIds.Group, new { groupId = groupId }, version: s_currentApiVersion, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> AddMemberToGroupAsyncInternal(object routeParams, IEnumerable<KeyValuePair<string, string>> query, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(IdentityResourceIds.AreaName, "AddMemberToGroup"))
            {
                //REST USAGE NON-STANDARD: This is modeled as a PUT operation, but contains no body. PUT should create or replace the resource at this
                //address, but in this case, there is no resource, it is adding a link between resources. This should be done differently
                return await SendAsync<bool>(HttpMethod.Put, IdentityResourceIds.Member, routeParams, version: s_currentApiVersion, queryParameters: query, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private void AppendQueryString(List<KeyValuePair<string, string>> queryParams, QueryMembership queryMembership, IEnumerable<string> propertyNameFilters, bool includeRestrictedVisibility)
        {
            queryParams.Add(QueryParameters.QueryMembership, queryMembership.ToString());

            queryParams.AddMultiple(QueryParameters.Properties, propertyNameFilters);

            if (includeRestrictedVisibility)
            {
                queryParams.Add(QueryParameters.IncludeRestrictedVisibility, "true");
            }
        }

        private static string SerializeDescriptor(IdentityDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return string.Empty;
            }
            else
            {
                return string.Join(";", descriptor.IdentityType, descriptor.Identifier);
            }
        }

      
        #endregion

        /// <summary>
        /// Exceptions for account errors
        /// </summary>
        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get
            {
                return s_translatedExceptions;
            }
        }

        private static class IdentityBatchTelemetryConstants
        {
            public const string QueryMembershipHint = "queryMembership";
            public const string FlavorHint = "flavor";
            public const string CountHint = "count";

            public const string ByIdFlavor = "id";
            public const string ByDescriptorFlavor = "descriptor";
            public const string BySubjectDescriptorFlavor = "subjectDescriptor";
            public const string BySocialDescriptorFlavor = "socialDescriptor";
        }

        private static Dictionary<string, Type> s_translatedExceptions;
        private static readonly ApiResourceVersion s_currentApiVersion;
        private const int maxDescriptors = 5 /* 256 for descriptor + 64 for type + 1 */;
        private const int maxIds = 50;
    }
}
