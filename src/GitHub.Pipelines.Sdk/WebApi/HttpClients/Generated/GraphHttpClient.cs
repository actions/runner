/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 *
 * ---------------------------------------------------------
 * Generated file, DO NOT EDIT
 * ---------------------------------------------------------
 *
 * See following wiki page for instructions on how to regenerate:
 *   https://aka.ms/azure-devops-client-generation
 *
 * Configuration file:
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\graph.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    [ResourceArea(GraphResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 20, failurePercentage: 80, MaxConcurrentRequests = 55)]
    public class GraphHttpClient : VssHttpClientBase
    {
        public GraphHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public GraphHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public GraphHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public GraphHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public GraphHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subjectDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task DeleteAvatarAsync(
            string subjectDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("801eaf9c-0585-4be8-9cdb-b0efa074de91");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subjectDescriptor"></param>
        /// <param name="size"></param>
        /// <param name="format"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Avatar> GetAvatarAsync(
            string subjectDescriptor,
            AvatarSize? size = null,
            string format = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("801eaf9c-0585-4be8-9cdb-b0efa074de91");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (size != null)
            {
                queryParams.Add("size", size.Value.ToString());
            }
            if (format != null)
            {
                queryParams.Add("format", format);
            }

            return SendAsync<Avatar>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="subjectDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task SetAvatarAsync(
            Avatar avatar,
            string subjectDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("801eaf9c-0585-4be8-9cdb-b0efa074de91");
            object routeValues = new { subjectDescriptor = subjectDescriptor };
            HttpContent content = new ObjectContent<Avatar>(avatar, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphCachePolicies> GetCachePoliciesAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("beb83272-b415-48e8-ac1e-a9b805760739");

            return SendAsync<GraphCachePolicies>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Resolve a storage key to a descriptor
        /// </summary>
        /// <param name="storageKey">Storage key of the subject (user, group, scope, etc.) to resolve</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphDescriptorResult> GetDescriptorAsync(
            Guid storageKey,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("048aee0a-7072-4cde-ab73-7af77b1e0b4e");
            object routeValues = new { storageKey = storageKey };

            return SendAsync<GraphDescriptorResult>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Acquires the full set of federated provider authentication data available for the given graph subject and provider name.
        /// </summary>
        /// <param name="subjectDescriptor">the descriptor of the graph subject that we should acquire data for</param>
        /// <param name="providerName">the name of the provider to acquire data for, e.g. "github.com"</param>
        /// <param name="versionHint">a version hint that can be used for optimistic cache concurrency and to support retries on access token failures; note that this is a hint only and does not guarantee a particular version on the response</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphFederatedProviderData> GetFederatedProviderDataAsync(
            SubjectDescriptor subjectDescriptor,
            string providerName,
            long? versionHint = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("5dcd28d6-632d-477f-ac6b-398ea9fc2f71");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("providerName", providerName);
            if (versionHint != null)
            {
                queryParams.Add("versionHint", versionHint.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<GraphFederatedProviderData>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Create a new VSTS group or materialize an existing AAD group.
        /// </summary>
        /// <param name="creationContext">The subset of the full graph group used to uniquely find the graph subject in an external provider.</param>
        /// <param name="scopeDescriptor">A descriptor referencing the scope (collection, project) in which the group should be created. If omitted, will be created in the scope of the enclosing account or organization. Valid only for VSTS groups.</param>
        /// <param name="groupDescriptors">A comma separated list of descriptors referencing groups you want the graph group to join</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphGroup> CreateGroupAsync(
            GraphGroupCreationContext creationContext,
            string scopeDescriptor = null,
            IEnumerable<SubjectDescriptor> groupDescriptors = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("ebbe6af8-0b91-4c13-8cf1-777c14858188");
            HttpContent content = new ObjectContent<GraphGroupCreationContext>(creationContext, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (scopeDescriptor != null)
            {
                queryParams.Add("scopeDescriptor", scopeDescriptor);
            }
            if (groupDescriptors != null && groupDescriptors.Any())
            {
                queryParams.Add("groupDescriptors", string.Join(",", groupDescriptors));
            }

            return SendAsync<GraphGroup>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Removes a VSTS group from all of its parent groups.
        /// </summary>
        /// <param name="groupDescriptor">The descriptor of the group to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task DeleteGroupAsync(
            string groupDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("ebbe6af8-0b91-4c13-8cf1-777c14858188");
            object routeValues = new { groupDescriptor = groupDescriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Get a group by its descriptor.
        /// </summary>
        /// <param name="groupDescriptor">The descriptor of the desired graph group.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphGroup> GetGroupAsync(
            string groupDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ebbe6af8-0b91-4c13-8cf1-777c14858188");
            object routeValues = new { groupDescriptor = groupDescriptor };

            return SendAsync<GraphGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of all groups in the current scope (usually organization or account).
        /// </summary>
        /// <param name="scopeDescriptor">Specify a non-default scope (collection, project) to search for groups.</param>
        /// <param name="subjectTypes">A comma separated list of user subject subtypes to reduce the retrieved results, e.g. Microsoft.IdentityModel.Claims.ClaimsIdentity</param>
        /// <param name="continuationToken">An opaque data blob that allows the next page of data to resume immediately after where the previous page ended. The only reliable way to know if there is more data left is the presence of a continuation token.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task<PagedGraphGroups> ListGroupsAsync(
            string scopeDescriptor = null,
            IEnumerable<string> subjectTypes = null,
            string continuationToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ebbe6af8-0b91-4c13-8cf1-777c14858188");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (scopeDescriptor != null)
            {
                queryParams.Add("scopeDescriptor", scopeDescriptor);
            }
            if (subjectTypes != null && subjectTypes.Any())
            {
                queryParams.Add("subjectTypes", string.Join(",", subjectTypes));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "application/json",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                PagedGraphGroups returnObject = new PagedGraphGroups();
                using (HttpResponseMessage response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    returnObject.ContinuationToken = GetHeaderValue(response, "X-MS-ContinuationToken");
                    returnObject.GraphGroups = await ReadContentAsAsync<List<GraphGroup>>(response, cancellationToken).ConfigureAwait(false);
                }
                return returnObject;
            }
        }

        /// <summary>
        /// [Preview API] Update the properties of a VSTS group.
        /// </summary>
        /// <param name="groupDescriptor">The descriptor of the group to modify.</param>
        /// <param name="patchDocument">The JSON+Patch document containing the fields to alter.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphGroup> UpdateGroupAsync(
            string groupDescriptor,
            JsonPatchDocument patchDocument,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("ebbe6af8-0b91-4c13-8cf1-777c14858188");
            object routeValues = new { groupDescriptor = groupDescriptor };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            return SendAsync<GraphGroup>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="memberLookup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<IReadOnlyDictionary<SubjectDescriptor, GraphMember>> LookupMembersAsync(
            GraphSubjectLookup memberLookup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("3d74d524-ae3d-4d24-a9a7-f8a5cf82347a");
            HttpContent content = new ObjectContent<GraphSubjectLookup>(memberLookup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<IReadOnlyDictionary<SubjectDescriptor, GraphMember>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] This endpoint returns a result for any member that has ever been valid in the system, even if the member has since been deleted or has had all their memberships deleted. The current validity of the member is indicated through its disabled property, which is omitted when false.
        /// </summary>
        /// <param name="memberDescriptor">The descriptor of the desired member.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphMember> GetMemberByDescriptorAsync(
            string memberDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("b9af63a7-5db6-4af8-aae7-387f775ea9c6");
            object routeValues = new { memberDescriptor = memberDescriptor };

            return SendAsync<GraphMember>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Create a new membership between a container and subject.
        /// </summary>
        /// <param name="subjectDescriptor">A descriptor to a group or user that can be the child subject in the relationship.</param>
        /// <param name="containerDescriptor">A descriptor to a group that can be the container in the relationship.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphMembership> AddMembershipAsync(
            string subjectDescriptor,
            string containerDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("3fd2e6ca-fb30-443a-b579-95b19ed0934c");
            object routeValues = new { subjectDescriptor = subjectDescriptor, containerDescriptor = containerDescriptor };

            return SendAsync<GraphMembership>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Check to see if a membership relationship between a container and subject exists.
        /// </summary>
        /// <param name="subjectDescriptor">The group or user that is a child subject of the relationship.</param>
        /// <param name="containerDescriptor">The group that is the container in the relationship.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task<bool> CheckMembershipExistenceAsync(
            string subjectDescriptor,
            string containerDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("HEAD");
            Guid locationId = new Guid("3fd2e6ca-fb30-443a-b579-95b19ed0934c");
            object routeValues = new { subjectDescriptor = subjectDescriptor, containerDescriptor = containerDescriptor };

            try
            {
                await SendAsync(
                    httpMethod,
                    locationId,
                    routeValues: routeValues,
                    version: new ApiResourceVersion("5.1-preview.1"),
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
                
            }
            catch(VssServiceResponseException ex)
            {
                if (ex.HttpStatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// [Preview API] Get a membership relationship between a container and subject.
        /// </summary>
        /// <param name="subjectDescriptor">A descriptor to the child subject in the relationship.</param>
        /// <param name="containerDescriptor">A descriptor to the container in the relationship.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphMembership> GetMembershipAsync(
            string subjectDescriptor,
            string containerDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3fd2e6ca-fb30-443a-b579-95b19ed0934c");
            object routeValues = new { subjectDescriptor = subjectDescriptor, containerDescriptor = containerDescriptor };

            return SendAsync<GraphMembership>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Deletes a membership between a container and subject.
        /// </summary>
        /// <param name="subjectDescriptor">A descriptor to a group or user that is the child subject in the relationship.</param>
        /// <param name="containerDescriptor">A descriptor to a group that is the container in the relationship.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RemoveMembershipAsync(
            string subjectDescriptor,
            string containerDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("3fd2e6ca-fb30-443a-b579-95b19ed0934c");
            object routeValues = new { subjectDescriptor = subjectDescriptor, containerDescriptor = containerDescriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Get all the memberships where this descriptor is a member in the relationship.
        /// </summary>
        /// <param name="subjectDescriptor">Fetch all direct memberships of this descriptor.</param>
        /// <param name="direction">Defaults to Up.</param>
        /// <param name="depth">The maximum number of edges to traverse up or down the membership tree. Currently the only supported value is '1'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<GraphMembership>> ListMembershipsAsync(
            string subjectDescriptor,
            GraphTraversalDirection? direction = null,
            int? depth = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e34b6394-6b30-4435-94a9-409a5eef3e31");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (direction != null)
            {
                queryParams.Add("direction", direction.Value.ToString());
            }
            if (depth != null)
            {
                queryParams.Add("depth", depth.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<GraphMembership>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Check whether a subject is active or inactive.
        /// </summary>
        /// <param name="subjectDescriptor">Descriptor of the subject (user, group, scope, etc.) to check state of</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphMembershipState> GetMembershipStateAsync(
            string subjectDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1ffe5c94-1144-4191-907b-d0211cad36a8");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            return SendAsync<GraphMembershipState>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Traverse memberships of the given subject descriptors.
        /// </summary>
        /// <param name="membershipTraversalLookup">Fetch the descendants/ancestors of the list of descriptors depending on direction.</param>
        /// <param name="direction">The default value is Unknown.</param>
        /// <param name="depth">The default value is '1'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<IReadOnlyDictionary<SubjectDescriptor, GraphMembershipTraversal>> LookupMembershipTraversalsAsync(
            GraphSubjectLookup membershipTraversalLookup,
            GraphTraversalDirection? direction = null,
            int? depth = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("5d59d874-746f-4f9b-9459-0e571f1ded8c");
            HttpContent content = new ObjectContent<GraphSubjectLookup>(membershipTraversalLookup, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (direction != null)
            {
                queryParams.Add("direction", direction.Value.ToString());
            }
            if (depth != null)
            {
                queryParams.Add("depth", depth.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<IReadOnlyDictionary<SubjectDescriptor, GraphMembershipTraversal>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Traverse memberships of the given subject descriptor.
        /// </summary>
        /// <param name="subjectDescriptor">Fetch the descendants/ancestors of this descriptor depending on direction.</param>
        /// <param name="direction">The default value is Unknown.</param>
        /// <param name="depth">The default value is '1'.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphMembershipTraversal> TraverseMembershipsAsync(
            string subjectDescriptor,
            GraphTraversalDirection? direction = null,
            int? depth = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("5d59d874-746f-4f9b-9459-0e571f1ded8c");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (direction != null)
            {
                queryParams.Add("direction", direction.Value.ToString());
            }
            if (depth != null)
            {
                queryParams.Add("depth", depth.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<GraphMembershipTraversal>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphProviderInfo> GetProviderInfoAsync(
            string userDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1e377995-6fa2-4588-bd64-930186abdcfa");
            object routeValues = new { userDescriptor = userDescriptor };

            return SendAsync<GraphProviderInfo>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="creationContext"></param>
        /// <param name="scopeDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphScope> CreateScopeAsync(
            GraphScopeCreationContext creationContext,
            string scopeDescriptor = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("21b5fea7-2513-41d0-af78-b8cdb0f328bb");
            object routeValues = new { scopeDescriptor = scopeDescriptor };
            HttpContent content = new ObjectContent<GraphScopeCreationContext>(creationContext, new VssJsonMediaTypeFormatter(true));

            return SendAsync<GraphScope>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="scopeDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task DeleteScopeAsync(
            string scopeDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("21b5fea7-2513-41d0-af78-b8cdb0f328bb");
            object routeValues = new { scopeDescriptor = scopeDescriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Get a scope identified by its descriptor
        /// </summary>
        /// <param name="scopeDescriptor">A descriptor that uniquely identifies a scope.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphScope> GetScopeAsync(
            string scopeDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("21b5fea7-2513-41d0-af78-b8cdb0f328bb");
            object routeValues = new { scopeDescriptor = scopeDescriptor };

            return SendAsync<GraphScope>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="scopeDescriptor"></param>
        /// <param name="patchDocument"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task UpdateScopeAsync(
            string scopeDescriptor,
            JsonPatchDocument patchDocument,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("21b5fea7-2513-41d0-af78-b8cdb0f328bb");
            object routeValues = new { scopeDescriptor = scopeDescriptor };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Resolve a descriptor to a storage key.
        /// </summary>
        /// <param name="subjectDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphStorageKeyResult> GetStorageKeyAsync(
            string subjectDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("eb85f8cc-f0f6-4264-a5b1-ffe2e4d4801f");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            return SendAsync<GraphStorageKeyResult>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Resolve descriptors to users, groups or scopes (Subjects) in a batch.
        /// </summary>
        /// <param name="subjectLookup">A list of descriptors that specifies a subset of subjects to retrieve. Each descriptor uniquely identifies the subject across all instance scopes, but only at a single point in time.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<IReadOnlyDictionary<SubjectDescriptor, GraphSubject>> LookupSubjectsAsync(
            GraphSubjectLookup subjectLookup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("4dd4d168-11f2-48c4-83e8-756fa0de027c");
            HttpContent content = new ObjectContent<GraphSubjectLookup>(subjectLookup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<IReadOnlyDictionary<SubjectDescriptor, GraphSubject>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subjectDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<GraphSubject> GetSubjectAsync(
            string subjectDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1d44a2ac-4f8a-459e-83c2-1c92626fb9c6");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            return SendAsync<GraphSubject>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Materialize an existing AAD or MSA user into the VSTS account.
        /// </summary>
        /// <param name="creationContext">The subset of the full graph user used to uniquely find the graph subject in an external provider.</param>
        /// <param name="groupDescriptors">A comma separated list of descriptors of groups you want the graph user to join</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphUser> CreateUserAsync(
            GraphUserCreationContext creationContext,
            IEnumerable<SubjectDescriptor> groupDescriptors = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("005e26ec-6b77-4e4f-a986-b3827bf241f5");
            HttpContent content = new ObjectContent<GraphUserCreationContext>(creationContext, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (groupDescriptors != null && groupDescriptors.Any())
            {
                queryParams.Add("groupDescriptors", string.Join(",", groupDescriptors));
            }

            return SendAsync<GraphUser>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Disables a user.
        /// </summary>
        /// <param name="userDescriptor">The descriptor of the user to delete.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task DeleteUserAsync(
            string userDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("005e26ec-6b77-4e4f-a986-b3827bf241f5");
            object routeValues = new { userDescriptor = userDescriptor };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Get a user by its descriptor.
        /// </summary>
        /// <param name="userDescriptor">The descriptor of the desired user.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphUser> GetUserAsync(
            string userDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("005e26ec-6b77-4e4f-a986-b3827bf241f5");
            object routeValues = new { userDescriptor = userDescriptor };

            return SendAsync<GraphUser>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of all users in a given scope.
        /// </summary>
        /// <param name="subjectTypes">A comma separated list of user subject subtypes to reduce the retrieved results, e.g. msa’, ‘aad’, ‘svc’ (service identity), ‘imp’ (imported identity), etc.</param>
        /// <param name="continuationToken">An opaque data blob that allows the next page of data to resume immediately after where the previous page ended. The only reliable way to know if there is more data left is the presence of a continuation token.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task<PagedGraphUsers> ListUsersAsync(
            IEnumerable<string> subjectTypes = null,
            string continuationToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("005e26ec-6b77-4e4f-a986-b3827bf241f5");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (subjectTypes != null && subjectTypes.Any())
            {
                queryParams.Add("subjectTypes", string.Join(",", subjectTypes));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("5.1-preview.1"),
                queryParameters: queryParams,
                mediaType: "application/json",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                PagedGraphUsers returnObject = new PagedGraphUsers();
                using (HttpResponseMessage response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    returnObject.ContinuationToken = GetHeaderValue(response, "X-MS-ContinuationToken");
                    returnObject.GraphUsers = await ReadContentAsAsync<List<GraphUser>>(response, cancellationToken).ConfigureAwait(false);
                }
                return returnObject;
            }
        }

        /// <summary>
        /// [Preview API] Map an existing user to a different identity
        /// </summary>
        /// <param name="updateContext">The subset of the full graph user used to uniquely find the graph subject in an external provider.</param>
        /// <param name="userDescriptor">the descriptor of the user to update</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GraphUser> UpdateUserAsync(
            GraphUserUpdateContext updateContext,
            string userDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("005e26ec-6b77-4e4f-a986-b3827bf241f5");
            object routeValues = new { userDescriptor = userDescriptor };
            HttpContent content = new ObjectContent<GraphUserUpdateContext>(updateContext, new VssJsonMediaTypeFormatter(true));

            return SendAsync<GraphUser>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }
    }
}
