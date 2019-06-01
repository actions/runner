using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class WebApiResources
    {
        public static string UnsupportedContentType(params object[] args)
        {
            const string Format = @"The server returns content type {0}, which is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DownloadCorrupted(params object[] args)
        {
            const string Format = @"The download file is corrupted. Get the file again.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SerializingPhrase(params object[] args)
        {
            const string Format = @"being serialized";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DeserializationCorrupt(params object[] args)
        {
            const string Format = @"The data presented for deserialization to the PropertiesCollection is corrupt.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ClientResourceVersionNotSupported(params object[] args)
        {
            const string Format = @"The server does not support resource {0} at API version {1}. The minimum supported version on {2} is {3}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ResourceNotFoundOnServerMessage(params object[] args)
        {
            const string Format = @"API resource location {0} is not registered on {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ResourceNotRegisteredMessage(params object[] args)
        {
            const string Format = @"API resource location {0} is not registered on this server.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerIdMustBeGreaterThanZero(params object[] args)
        {
            const string Format = @"The container ID must be greater than zero.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string FullyQualifiedLocationParameter(params object[] args)
        {
            const string Format = @"The value of the location parameter cannot be null if the RelativeToSetting is 'FullyQualified'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RelativeLocationMappingErrorMessage(params object[] args)
        {
            const string Format = @"TF205038: You cannot add location mappings to service definitions that are not part of the FullyQualified type.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidAccessMappingLocationServiceUrl(params object[] args)
        {
            const string Format = @"TF205035: The access mapping is not valid and cannot be registered. The location service URL cannot be null or empty.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ServiceDefinitionDoesNotExist(params object[] args)
        {
            const string Format = @"The service definition with service type '{0}' and identifier '{1}' does not exist.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ServiceDefinitionWithNoLocations(params object[] args)
        {
            const string Format = @"TF205046: The service with the following type does not have a location mapping: {0}. You must provide at least one location in order to configure locations for an external service.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string JsonParseError(params object[] args)
        {
            const string Format = @"Unable to parse JSON in: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MissingRequiredParameterMessage(params object[] args)
        {
            const string Format = @"A required parameter {0} was not specified for this request.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ProxyAuthenticationRequired(params object[] args)
        {
            const string Format = @"SP324097: Your network proxy requires authentication.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidApiVersionStringMessage(params object[] args)
        {
            const string Format = @"Invalid api version string: ""{0}"". Api version string must be in the format: {{Major}}.{{Minor}}[-preview[.{{ResourceVersion}}]].";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ApiResourceDuplicateIdMessage(params object[] args)
        {
            const string Format = @"The following location id has already been registered: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ApiResourceDuplicateRouteNameMessage(params object[] args)
        {
            const string Format = @"The following route name has already been registered: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RequestContentTypeNotSupported(params object[] args)
        {
            const string Format = @"The request indicated a Content-Type of ""{0}"" for method type ""{1}"" which is not supported. Valid content types for this method are: {2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidReferenceLinkFormat(params object[] args)
        {
            const string Format = @"ReferenceLinks is a dictionary that contains either a single ReferenceLink or an array of ReferenceLinks.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PreviewVersionNotSuppliedMessage(params object[] args)
        {
            const string Format = @"The requested version ""{0}"" of the resource is under preview. The -preview flag must be supplied in the api-version for such requests. For example: ""{0}-preview""";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VersionNotSuppliedMessage(params object[] args)
        {
            const string Format = @"No api-version was supplied for the ""{0}"" request. The version must be supplied either as part of the Accept header (e.g. ""application/json; api-version=1.0"") or as a query parameter (e.g. ""?api-version=1.0"").";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInvalidEndBlock(params object[] args)
        {
            const string Format = @"Unexpected end block '{0}' before any start block";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateMissingBlockHelper(params object[] args)
        {
            const string Format = @"Block Helper '{0}' not found for expression '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateMissingHelper(params object[] args)
        {
            const string Format = @"Helper '{0}' not found for expression '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateNonMatchingEndBlock(params object[] args)
        {
            const string Format = @"End block '{0}' does not match start block '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateBraceCountMismatch(params object[] args)
        {
            const string Format = @"The expression '{0}' is invalid due to mismatching start and end brace count.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInvalidEndBraces(params object[] args)
        {
            const string Format = @"Invalid end braces before start braces at position '{0}' of template '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInvalidStartBraces(params object[] args)
        {
            const string Format = @"Invalid start braces within template expression '{0}' at position {1} of template '{2}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInvalidEscapedStringLiteral(params object[] args)
        {
            const string Format = @"Invalid escape character in string literal '{0}' within template expression '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateUnterminatedStringLiteral(params object[] args)
        {
            const string Format = @"Unterminated string literal '{0}' within template expression '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInvalidNumericLiteral(params object[] args)
        {
            const string Format = @"Invalid numeric literal '{0}' within template expression '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OperationNotFoundException(params object[] args)
        {
            const string Format = @"Failed to find operation '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OperationPluginNotFoundException(params object[] args)
        {
            const string Format = @"Failed to find operation plugin '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OperationPluginWithSameIdException(params object[] args)
        {
            const string Format = @"Found several plugins for the id '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OperationPluginNoPermission(params object[] args)
        {
            const string Format = @"The operation '{1}' for the plugin '{0}' doesn't have permission.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OperationUpdateException(params object[] args)
        {
            const string Format = @"Operation update for operation '{0}' did not complete successfully.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CollectionDoesNotExistException(params object[] args)
        {
            const string Format = @"VS402844: Collection with name {0} does not exist.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MissingCloseInlineMessage(params object[] args)
        {
            const string Format = @"Missing close expression for inline content.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MissingEndingBracesMessage(params object[] args)
        {
            const string Format = @"No ending braces for expression '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NestedInlinePartialsMessage(params object[] args)
        {
            const string Format = @"An inline partial cannot contain another inline partial";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GetServiceArgumentError(params object[] args)
        {
            const string Format = @"TF400776: '{0}' must be a non-abstract class with a public parameterless or default constructor in order to use it as parameter 'T' in GetService<T>().";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExtensibleServiceTypeNotRegistered(params object[] args)
        {
            const string Format = @"The service type '{0}' does not have a registered implementation or default implementation attribute.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExtensibleServiceTypeNotValid(params object[] args)
        {
            const string Format = @"'{1}' does not extend or implement the service type '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ServerDataProviderNotFound(params object[] args)
        {
            const string Format = @"The server data provider for service owner {0} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ClientCertificateMissing(params object[] args)
        {
            const string Format = @"No certificate capable of client authentication was found in the certificate store with thumbprint {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SmartCardMissing(params object[] args)
        {
            const string Format = @"The smart card containing the private key for the certificate with thumbprint {0} is not available.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ClientCertificateNoPermission(params object[] args)
        {
            const string Format = @"The certificate with thumbprint {0} could not be used for client authentication. The current user may not have permission to use the certificate.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ClientCertificateErrorReadingStore(params object[] args)
        {
            const string Format = @"An exception occurred while loading client authentication certificates from the certificate store: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CannotAuthenticateAsAnotherUser(params object[] args)
        {
            const string Format = @"We were unable to establish the connection because it is configured for user {0} but you attempted to connect using user {1}. To connect as a different user perform a switch user operation. To connect with the configured identity just attempt the last operation again.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInvalidPartialReference(params object[] args)
        {
            const string Format = @"Invalid partial reference: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CannotGetUnattributedClient(params object[] args)
        {
            const string Format = @"The current VssConnection does not support calling GetClient for this client type: '{0}'. Instead, use the GetClient overload which accepts a serviceIdentifier parameter to specify the intended target service for the given client.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnknownEntityType(params object[] args)
        {
            const string Format = @"Unknown entityType {0}. Cannot parse.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GraphGroupMissingRequiredFields(params object[] args)
        {
            const string Format = @"Must have exactly one of originId, principlaName or displayName set.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GraphUserMissingRequiredFields(params object[] args)
        {
            const string Format = @"Must have exactly one of originId or principlaName set.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheEvaluationResultLengthExceeded(params object[] args)
        {
            const string Format = @"The maximum evaluation result length has been exceeded. The maximum allowed length is {0:N0} characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateInlinePartialsNotAllowed(params object[] args)
        {
            const string Format = @"Inline partial expressions are not allowed";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MustacheTemplateMaxDepthExceeded(params object[] args)
        {
            const string Format = @"The maximum expression depth has been exceeded. The maximum allowed expression depth is {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedTokenType(params object[] args)
        {
            const string Format = @"Unexpected token type. Only JObject, JArrays, Guid, String and Boolean are supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ApiVersionOutOfRange(params object[] args)
        {
            const string Format = @"The requested REST API version of {0} is out of range for this server. The latest REST API version this server supports is {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ApiVersionOutOfRangeForRoute(params object[] args)
        {
            const string Format = @"The request matched route {1}, but the requested REST API version {0} was outside the valid version range for this route.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ApiVersionOutOfRangeForRoutes(params object[] args)
        {
            const string Format = @"The following routes matched, but the requested REST API version {0} was outside the valid version ranges: {1}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnsafeCrossOriginRequest(params object[] args)
        {
            const string Format = @"A cross-origin request from origin ""{0}"" is not allowed when using cookie-based authentication. An authentication token needs to be provided in the Authorization header of the request.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
