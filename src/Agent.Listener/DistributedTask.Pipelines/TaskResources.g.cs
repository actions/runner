using System.Globalization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines
{
    internal static class TaskResources
    {
            internal static string PlanNotFound(params object[] args)
            {
                const string Format = @"No plan found for identifier {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string PlanSecurityDeleteError(params object[] args)
            {
                const string Format = @"Access denied: {0} does not have delete permissions for orchestration plan {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string PlanSecurityWriteError(params object[] args)
            {
                const string Format = @"Access denied: {0} does not have write permissions for orchestration plan {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string HubExtensionNotFound(params object[] args)
            {
                const string Format = @"No task hub extension was found for hub {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string SecurityTokenNotFound(params object[] args)
            {
                const string Format = @"No security token was found for artifact {0} using extension {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TimelineNotFound(params object[] args)
            {
                const string Format = @"No timeline found for plan {0} with identifier {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string LogWithNoContentError(params object[] args)
            {
                const string Format = @"Content must be provided to create a log.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string LogWithNoContentLengthError(params object[] args)
            {
                const string Format = @"ContentLength header must be specified to create a log.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UnsupportedRollbackContainers(params object[] args)
            {
                const string Format = @"Rollback is supported only at the top level container.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string HubNotFound(params object[] args)
            {
                const string Format = @"No hub is registered with name {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string MultipleHubResolversNotSupported(params object[] args)
            {
                const string Format = @"Only one default task hub resolver may be specified per application. Found {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string HubExists(params object[] args)
            {
                const string Format = @"A hub is already registered with name {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TimelineRecordInvalid(params object[] args)
            {
                const string Format = @"The timeline record {0} is not valid. Name and Type are required fields.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TimelineRecordNotFound(params object[] args)
            {
                const string Format = @"No timeline record found for plan {0} and timeline {1} with identifier {2}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string FailedToObtainJobAuthorization(params object[] args)
            {
                const string Format = @"Unable to obtain an authenticated token for running job {0} with plan type {1} and identifier {2}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TaskInputRequired(params object[] args)
            {
                const string Format = @"Task {0} did not specify a value for required input {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string PlanOrchestrationTerminated(params object[] args)
            {
                const string Format = @"Orchestration plan {0} is not in a runnable state.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string PlanAlreadyStarted(params object[] args)
            {
                const string Format = @"Orchestration plan {0} version {1} has already been started for hub {2}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TimelineExists(params object[] args)
            {
                const string Format = @"A timeline already exists for plan {0} with identifier {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidContainer(params object[] args)
            {
                const string Format = @"Container is not valid for {0} orchestration.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string EndpointNotFound(params object[] args)
            {
                const string Format = @"No endpoint found with identifier {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string ShouldStartWithEndpointUrl(params object[] args)
            {
                const string Format = @"EndpointUrl of HttpRequest execution should start with $(endpoint.url).";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TaskExecutionDefinitionInvalid(params object[] args)
            {
                const string Format = @"Task execution section of task definition for Id : {0} is either missing or not valid.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string ServerExecutionFailure(params object[] args)
            {
                const string Format = @"Failure occured while sending Http Request : {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UnsupportedTaskCountForServerJob(params object[] args)
            {
                const string Format = @"Container is not valid for orchestration as job should contain exactly one task.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TaskServiceBusPublishFailed(params object[] args)
            {
                const string Format = @"Task {0} failed to publish to message bus {1} configured on service endpoint {2}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TaskServiceBusExecutionFailure(params object[] args)
            {
                const string Format = @"Task {0} failed to publish to message bus {1} configured on service endpoint {2}. Error: {3}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TimeoutFormatNotValid(params object[] args)
            {
                const string Format = @"The Timeout values '{0}' are not valid for job events '{1}' in the task execution section for Id: '{2}'. Specify valid timeout in 'hh:mm:ss' format such as '01:40:00' and try again.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string JobNotFound(params object[] args)
            {
                const string Format = @"No job found with identifier '{0}' for plan '{1}'.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string PlanGroupNotFound(params object[] args)
            {
                const string Format = @"No plan group found with identifier {0}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string PlanSecurityReadError(params object[] args)
            {
                const string Format = @"Access denied: {0} does not have read permissions for orchestration plan {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string SaveJobOutputVariablesError(params object[] args)
            {
                const string Format = @"Failed to save output variables for job '{0}'. Error: {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VstsAccessTokenCacheKeyLookupResultIsInvalidError(params object[] args)
            {
                const string Format = @"Failed to get  Visual Studio Team Foundation Service access token from property cache service, cache key is invalid.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VstsAccessTokenKeyNotFoundError(params object[] args)
            {
                const string Format = @"Visual Studio Team Foundation Service token (AccessTokenKey) is invalid. Try to validate again.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VstsAccessTokenCacheKeyLookupResultIsNullError(params object[] args)
            {
                const string Format = @"Failed to get  Visual Studio Team Foundation Service access token, cache value is invalid.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VstsAccessTokenIsNullError(params object[] args)
            {
                const string Format = @"Visual Studio Team Foundation Service access token is invalid, token shouldn't be null.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VstsIdTokenKeyNotFoundError(params object[] args)
            {
                const string Format = @"Visual Studio Team Foundation Service token (IdToken) is invalid. Try to validate again.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VstsNonceNotFoundError(params object[] args)
            {
                const string Format = @"Visual Studio Team Foundation Service token (Nonce) is invalid. Try to validate again.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string FailedToGenerateToken(params object[] args)
            {
                const string Format = @"Unable to generate a personal access token for service identity '{0}' ('{1}'). Error : {2}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string FailedToObtainToken(params object[] args)
            {
                const string Format = @"Failed to obtain the Json Web Token(JWT) for service principal id '{0}'";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidAzureEndpointAuthorizer(params object[] args)
            {
                const string Format = @"No Azure endpoint authorizer found for authentication of type '{0}'";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidAzureManagementCertificate(params object[] args)
            {
                const string Format = @"Invalid Azure Management certificate";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidEndpointAuthorizer(params object[] args)
            {
                const string Format = @"No Endpoint Authorizer found for endpoint of type '{0}'";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidEndpointId(params object[] args)
            {
                const string Format = @"The value {0} provided for the endpoint identifier is not within the permissible values for it.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidScopeId(params object[] args)
            {
                const string Format = @"The scope {0} is not valid.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string ResourceUrlNotSupported(params object[] args)
            {
                const string Format = @"ResourceUrl is not support for the endpoint type {0} and authentication scheme {1}.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string NoAzureCertificate(params object[] args)
            {
                const string Format = @"Could not extract certificate for AzureSubscription.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string NoAzureServicePrincipal(params object[] args)
            {
                const string Format = @"Could not extract service principal information for AzureSubscription.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string NoUsernamePassword(params object[] args)
            {
                const string Format = @"Could not extract Username and Password for endpoint.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string NullSessionToken(params object[] args)
            {
                const string Format = @"Unable to generate a personal access token for service identity '{0}' ('{1}') because of null result.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string MissingProperty(params object[] args)
            {
                const string Format = @"""Expected property {0} in service endpoint defintion""";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string ServiceEndPointNotFound(params object[] args)
            {
                const string Format = @"Service endpoint with id {0} not found";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidLicenseHub(params object[] args)
            {
                const string Format = @"This operation is not supported on {0} hub as it is not a licensing hub.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string HttpMethodNotRecognized(params object[] args)
            {
                const string Format = @"The Http Method: {0} is not supported.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TaskDefinitionInvalid(params object[] args)
            {
                const string Format = @"Task definition for Id: {0} is either missing or not valid.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UrlCannotBeEmpty(params object[] args)
            {
                const string Format = @"Url for HttpRequest cannot be empty.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UrlIsNotCorrect(params object[] args)
            {
                const string Format = @"Url {0} for HttpRequest is not correct.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UrlShouldComeFromEndpointOrExplicitelySpecified(params object[] args)
            {
                const string Format = @"Url for HttpRequest should either come from endpoint or specified explicitely.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string WaitForCompletionInvalid(params object[] args)
            {
                const string Format = @"Wait for completion can only be true or false. Current value: {0}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string HttpRequestTimeoutError(params object[] args)
            {
                const string Format = @"The request timed out after {0} seconds as no response was recieved.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UnableToAcquireLease(params object[] args)
            {
                const string Format = @"Unable to acquire lease: {0}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string UnableToCompleteOperationSecurely(params object[] args)
            {
                const string Format = @"Internal Error: Unable to complete operation securely.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string CancellingHttpRequestException(params object[] args)
            {
                const string Format = @"An error was encountered while cancelling request. Exception: {0}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string EncryptionKeyNotFound(params object[] args)
            {
                const string Format = @"Encryption Key should have been present in the drawer: {0} with lookup key: {1}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string ProcessingHttpRequestException(params object[] args)
            {
                const string Format = @"An error was encountered while processing request. Exception: {0}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string AzureKeyVaultTaskName(params object[] args)
            {
                const string Format = @"Download secrets: {0}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string AzureKeyVaultServiceEndpointIdMustBeValidGuid(params object[] args)
            {
                const string Format = @"Azure Key Vault provider's service endpoint id must be non empty and a valid guid.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string AzureKeyVaultKeyVaultNameMustBeValid(params object[] args)
            {
                const string Format = @"Azure Key Vault provider's vault name must be non empty.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string AzureKeyVaultLastRefreshedOnMustBeValid(params object[] args)
            {
                const string Format = @"Azure Key Vault provider's last refreshed on must be valid datetime.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string InvalidAzureKeyVaultVariableGroupProviderData(params object[] args)
            {
                const string Format = @"Either variable group is not an azure key vault variable group or invalid provider data in azure key vault variable group.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string VariableGroupTypeNotSupported(params object[] args)
            {
                const string Format = @"Variable group type {0} is not supported.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string TaskRequestMessageTypeNotSupported(params object[] args)
            {
                const string Format = @"This kind of message type: {0}, is not yet supported";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string HttpHandlerUnableToProcessError(params object[] args)
            {
                const string Format = @"Unable to process messages with count : {0} and message types as: {1}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string YamlFrontMatterNotClosed(params object[] args)
            {
                const string Format = @"Unexpected end of file '{0}'. The file started with '---' to indicate a preamble data section. The end of the file was reached without finding a corresponding closing '---'.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string YamlFrontMatterNotValid(params object[] args)
            {
                const string Format = @"Error parsing preamble data section from file '{0}'. {1}";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string YamlFileCount(params object[] args)
            {
                const string Format = @"A YAML definition may not exceed {0} file references.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
            internal static string MustacheEvaluationTimeout(params object[] args)
            {
                const string Format = @"YAML template preprocessing timed out for the file '{0}'. Template expansion cannot exceed '{1}' seconds.";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
    }
}
