//-----------------------------------------------------------------------
// <copyright file="DirectoryHttpClient.cs" company="Microsoft Corporation">
// Copyright (C) 2009-2014 All Rights Reserved
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.AadMemberAccessStatus;
using GitHub.Services.Common;
using GitHub.Services.Identity;
using GitHub.Services.WebApi;

namespace GitHub.Services.Directories.DirectoryService.Client
{
    [ResourceArea(DirectoryResourceIds.DirectoryService)]
    public class DirectoryHttpClient : VssHttpClientBase
    {
        private static Dictionary<String, Type> s_translatedExceptions;
        private static readonly ApiResourceVersion s_currentApiVersion;

        static DirectoryHttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();
            s_currentApiVersion = new ApiResourceVersion(1.0);
        }

        public DirectoryHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public DirectoryHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public DirectoryHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public DirectoryHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public DirectoryHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<IReadOnlyList<DirectoryEntityResult>> AddMembersAsync(
            IReadOnlyList<IDirectoryEntityDescriptor> members,
            string profile = null,
            string license = null,
            IEnumerable<string> localGroups = null,
            IEnumerable<string> propertiesToReturn = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PostAsync<IReadOnlyList<IDirectoryEntityDescriptor>, IReadOnlyList<DirectoryEntityResult>>(
                members,
                DirectoryResourceIds.Members,
                version: s_currentApiVersion,
                queryParameters: GetQueryParameters(profile, license, localGroups, propertiesToReturn),
                userState: userState,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DirectoryEntityResult> AddMemberAsync(
            IDirectoryEntityDescriptor member,
            string profile = null,
            string license = null,
            IEnumerable<string> localGroups = null,
            IEnumerable<string> propertiesToReturn = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PostAsync<IDirectoryEntityDescriptor, DirectoryEntityResult>(
                member,
                DirectoryResourceIds.Members,
                version: s_currentApiVersion,
                queryParameters: GetQueryParameters(profile, license, localGroups, propertiesToReturn),
                userState: userState,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DirectoryEntityResult> AddMemberAsync(
            string member,
            string profile = null,
            string license = null,
            IEnumerable<string> localGroups = null,
            IEnumerable<string> propertiesToReturn = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PostAsync<string, DirectoryEntityResult>(
                member,
                DirectoryResourceIds.Members,
                version: s_currentApiVersion,
                queryParameters: GetQueryParameters(profile, license, localGroups, propertiesToReturn),
                userState: userState,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        public async Task<AadMemberStatus> GetAadMemberStatusAsync(
            IdentityDescriptor identityDescriptor,
            Guid oid,
            Guid tenantId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(tenantId, "tenantId");
            ArgumentUtility.CheckForEmptyGuid(oid, "oid");
            ArgumentUtility.CheckForNull(identityDescriptor, "identityDescriptor");

            var queryParams = new List<KeyValuePair<String, String>>();
            queryParams.Add("tenantId", tenantId.ToString());
            queryParams.Add("identity", identityDescriptor.IdentityType + ";" + identityDescriptor.Identifier);

            object routeValues = new { objectId = oid.ToString() };

            using (new OperationScope(DirectoryResourceIds.DirectoryServiceArea, "DirectoryMemberStatus"))
            {
                return await GetAsync<AadMemberStatus>(
                    DirectoryResourceIds.MemberStatusLocationId,
                    version: s_currentApiVersion,
                    routeValues: routeValues,
                    queryParameters: queryParams,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<AadMemberStatus> GetAadMemberStatusAsync(
            SubjectDescriptor subjectDescriptor,
            Guid oid,
            Guid tenantId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(tenantId, "tenantId");
            ArgumentUtility.CheckForEmptyGuid(oid, "oid");
            ArgumentUtility.CheckForNull(subjectDescriptor, "subjectDescriptor");

            var queryParams = new List<KeyValuePair<String, String>>();
            queryParams.Add("tenantId", tenantId.ToString());
            queryParams.Add("identity", subjectDescriptor.ToString());

            object routeValues = new { objectId = oid.ToString() };

            using (new OperationScope(DirectoryResourceIds.DirectoryServiceArea, "DirectoryMemberStatus"))
            {
                return await GetAsync<AadMemberStatus>(
                    DirectoryResourceIds.MemberStatusLocationId,
                    version: s_currentApiVersion,
                    routeValues: routeValues,
                    queryParameters: queryParams,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }


        private List<KeyValuePair<String, String>> GetQueryParameters(
            string profile, 
            string license, 
            IEnumerable<string> localGroups,
            IEnumerable<string> propertiesToReturn)
        {
            var queryParameters = new List<KeyValuePair<String, String>>();

            if (profile != null)
            {
                queryParameters.Add(nameof(profile), profile);
            }

            if (license != null)
            {
                queryParameters.Add(nameof(license), license);
            }

            if (localGroups != null)
            {
                queryParameters.Add(nameof(localGroups), EnumerableToCsv(localGroups));
            }

            if (propertiesToReturn != null)
            {
                queryParameters.Add(nameof(propertiesToReturn), EnumerableToCsv(propertiesToReturn));
            }

            return queryParameters;
        }

        private string EnumerableToCsv(IEnumerable<string> values)
        {
            return values == null ? null : string.Join(",", values);
        }
    }
}
