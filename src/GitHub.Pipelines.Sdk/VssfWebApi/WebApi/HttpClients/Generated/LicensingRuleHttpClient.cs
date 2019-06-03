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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\licensingrule.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Graph.Client;
using GitHub.Services.Operations;
using GitHub.Services.WebApi;

namespace GitHub.Services.GroupLicensingRule
{
    [ResourceArea(LicensingRuleResourceIds.AreaId)]
    public class LicensingRuleHttpClient : VssHttpClientBase
    {
        public LicensingRuleHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public LicensingRuleHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public LicensingRuleHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public LicensingRuleHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public LicensingRuleHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Add a new group Licensing rule asynchronously
        /// </summary>
        /// <param name="licensingRule">The Licensing Rule</param>
        /// <param name="ruleOption">Rule Option</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<OperationReference> AddGroupLicensingRuleAsync(
            GroupLicensingRule licensingRule,
            RuleOption? ruleOption = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("1dae9af4-c85d-411b-b0c1-a46afaea1986");
            HttpContent content = new ObjectContent<GroupLicensingRule>(licensingRule, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ruleOption != null)
            {
                queryParams.Add("ruleOption", ruleOption.Value.ToString());
            }

            return SendAsync<OperationReference>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Delete a group Licensing rule
        /// </summary>
        /// <param name="subjectDescriptor">subjectDescriptor</param>
        /// <param name="ruleOption">Rule Option</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<OperationReference> DeleteGroupLicenseRuleAsync(
            string subjectDescriptor,
            RuleOption? ruleOption = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("1dae9af4-c85d-411b-b0c1-a46afaea1986");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ruleOption != null)
            {
                queryParams.Add("ruleOption", ruleOption.Value.ToString());
            }

            return SendAsync<OperationReference>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the group Licensing rule for the group with given SubjectDescriptor
        /// </summary>
        /// <param name="subjectDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<GroupLicensingRule> GetGroupLicensingRuleAsync(
            string subjectDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1dae9af4-c85d-411b-b0c1-a46afaea1986");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            return SendAsync<GroupLicensingRule>(
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
        /// <param name="top"></param>
        /// <param name="skip"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<GroupLicensingRule>> GetGroupLicensingRulesAsync(
            int top,
            int? skip = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1dae9af4-c85d-411b-b0c1-a46afaea1986");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("top", top.ToString(CultureInfo.InvariantCulture));
            if (skip != null)
            {
                queryParams.Add("skip", skip.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<GroupLicensingRule>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Update a group Licensing rule
        /// </summary>
        /// <param name="licensingRuleUpdate">The update model for the Licensing Rule</param>
        /// <param name="ruleOption">Rule Option</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<OperationReference> UpdateGroupLicensingRuleAsync(
            GroupLicensingRuleUpdate licensingRuleUpdate,
            RuleOption? ruleOption = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("1dae9af4-c85d-411b-b0c1-a46afaea1986");
            HttpContent content = new ObjectContent<GroupLicensingRuleUpdate>(licensingRuleUpdate, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ruleOption != null)
            {
                queryParams.Add("ruleOption", ruleOption.Value.ToString());
            }

            return SendAsync<OperationReference>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Applies group rules to the specified user
        /// </summary>
        /// <param name="ruleOption"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<OperationReference> ApplyGroupLicensingRulesToAllUsersAsync(
            RuleOption? ruleOption = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("14602853-288e-4711-a613-c3f27ffce285");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ruleOption != null)
            {
                queryParams.Add("ruleOption", ruleOption.Value.ToString());
            }

            return SendAsync<OperationReference>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets application status for the specific rule
        /// </summary>
        /// <param name="operationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<ApplicationStatus> GetApplicationStatusAsync(
            Guid? operationId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8953c613-d07f-43d3-a7bd-e9b66f960839");
            object routeValues = new { operationId = operationId };

            return SendAsync<ApplicationStatus>(
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
        /// <param name="fileId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<string> RetrieveFileAsync(
            int fileId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("c3c87024-5143-4631-94ce-cb2338b04bbc");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("fileId", fileId.ToString(CultureInfo.InvariantCulture));

            return SendAsync<string>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get Group License Rules for the given batch batch of group Ids
        /// </summary>
        /// <param name="groupRuleLookup"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<GroupLicensingRule>> LookupGroupLicensingRulesAsync(
            GraphSubjectLookup groupRuleLookup,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6282b958-792b-4f26-b5c8-6d035e02289f");
            HttpContent content = new ObjectContent<GraphSubjectLookup>(groupRuleLookup, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<GroupLicensingRule>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Applies group rules to the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task ApplyGroupLicensingRulesToUserAsync(
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("74a9de62-9afc-4a60-a6d9-f7c65e028619");
            object routeValues = new { userId = userId };

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
        /// [Preview API] Removes direct assignments from, and re-applies group rules to, the specified users
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RemoveDirectAssignmentAsync(
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("74a9de62-9afc-4a60-a6d9-f7c65e028619");
            object routeValues = new { userId = userId };

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
    }
}
