using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class WebApiResources
    {

        public static string UnsupportedContentType(object arg0)
        {
            const string Format = @"The server returns content type {0}, which is not supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string DownloadCorrupted()
        {
            const string Format = @"The download file is corrupted. Get the file again.";
            return Format;
        }

        public static string SerializingPhrase()
        {
            const string Format = @"being serialized";
            return Format;
        }

        public static string DeserializationCorrupt()
        {
            const string Format = @"The data presented for deserialization to the PropertiesCollection is corrupt.";
            return Format;
        }

        public static string ClientResourceVersionNotSupported(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"The server does not support resource {0} at API version {1}. The minimum supported version on {2} is {3}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string ResourceNotFoundOnServerMessage(object arg0, object arg1)
        {
            const string Format = @"API resource location {0} is not registered on {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ResourceNotRegisteredMessage(object arg0)
        {
            const string Format = @"API resource location {0} is not registered on this server.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerIdMustBeGreaterThanZero()
        {
            const string Format = @"The container ID must be greater than zero.";
            return Format;
        }

        public static string FullyQualifiedLocationParameter()
        {
            const string Format = @"The value of the location parameter cannot be null if the RelativeToSetting is 'FullyQualified'";
            return Format;
        }

        public static string RelativeLocationMappingErrorMessage()
        {
            const string Format = @"TF205038: You cannot add location mappings to service definitions that are not part of the FullyQualified type.";
            return Format;
        }

        public static string InvalidAccessMappingLocationServiceUrl()
        {
            const string Format = @"TF205035: The access mapping is not valid and cannot be registered. The location service URL cannot be null or empty.";
            return Format;
        }

        public static string ServiceDefinitionDoesNotExist(object arg0, object arg1)
        {
            const string Format = @"The service definition with service type '{0}' and identifier '{1}' does not exist.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ServiceDefinitionWithNoLocations(object arg0)
        {
            const string Format = @"TF205046: The service with the following type does not have a location mapping: {0}. You must provide at least one location in order to configure locations for an external service.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string JsonParseError(object arg0)
        {
            const string Format = @"Unable to parse JSON in: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MissingRequiredParameterMessage(object arg0)
        {
            const string Format = @"A required parameter {0} was not specified for this request.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ProxyAuthenticationRequired()
        {
            const string Format = @"SP324097: Your network proxy requires authentication.";
            return Format;
        }

        public static string InvalidApiVersionStringMessage(object arg0)
        {
            const string Format = @"Invalid api version string: ""{0}"". Api version string must be in the format: {{Major}}.{{Minor}}[-preview[.{{ResourceVersion}}]].";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ApiResourceDuplicateIdMessage(object arg0)
        {
            const string Format = @"The following location id has already been registered: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ApiResourceDuplicateRouteNameMessage(object arg0)
        {
            const string Format = @"The following route name has already been registered: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string RequestContentTypeNotSupported(object arg0, object arg1, object arg2)
        {
            const string Format = @"The request indicated a Content-Type of ""{0}"" for method type ""{1}"" which is not supported. Valid content types for this method are: {2}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string InvalidReferenceLinkFormat()
        {
            const string Format = @"ReferenceLinks is a dictionary that contains either a single ReferenceLink or an array of ReferenceLinks.";
            return Format;
        }

        public static string PreviewVersionNotSuppliedMessage(object arg0)
        {
            const string Format = @"The requested version ""{0}"" of the resource is under preview. The -preview flag must be supplied in the api-version for such requests. For example: ""{0}-preview""";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string VersionNotSuppliedMessage(object arg0)
        {
            const string Format = @"No api-version was supplied for the ""{0}"" request. The version must be supplied either as part of the Accept header (e.g. ""application/json; api-version=1.0"") or as a query parameter (e.g. ""?api-version=1.0"").";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MustacheTemplateInvalidEndBlock(object arg0)
        {
            const string Format = @"Unexpected end block '{0}' before any start block";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MustacheTemplateMissingBlockHelper(object arg0, object arg1)
        {
            const string Format = @"Block Helper '{0}' not found for expression '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateMissingHelper(object arg0, object arg1)
        {
            const string Format = @"Helper '{0}' not found for expression '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateNonMatchingEndBlock(object arg0, object arg1)
        {
            const string Format = @"End block '{0}' does not match start block '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateBraceCountMismatch(object arg0)
        {
            const string Format = @"The expression '{0}' is invalid due to mismatching start and end brace count.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MustacheTemplateInvalidEndBraces(object arg0, object arg1)
        {
            const string Format = @"Invalid end braces before start braces at position '{0}' of template '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateInvalidStartBraces(object arg0, object arg1, object arg2)
        {
            const string Format = @"Invalid start braces within template expression '{0}' at position {1} of template '{2}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string MustacheTemplateInvalidEscapedStringLiteral(object arg0, object arg1)
        {
            const string Format = @"Invalid escape character in string literal '{0}' within template expression '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateUnterminatedStringLiteral(object arg0, object arg1)
        {
            const string Format = @"Unterminated string literal '{0}' within template expression '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateInvalidNumericLiteral(object arg0, object arg1)
        {
            const string Format = @"Invalid numeric literal '{0}' within template expression '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string OperationNotFoundException(object arg0)
        {
            const string Format = @"Failed to find operation '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string OperationPluginNotFoundException(object arg0)
        {
            const string Format = @"Failed to find operation plugin '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string OperationPluginWithSameIdException(object arg0)
        {
            const string Format = @"Found several plugins for the id '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string OperationPluginNoPermission(object arg0, object arg1)
        {
            const string Format = @"The operation '{1}' for the plugin '{0}' doesn't have permission.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string OperationUpdateException(object arg0)
        {
            const string Format = @"Operation update for operation '{0}' did not complete successfully.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string CollectionDoesNotExistException(object arg0)
        {
            const string Format = @"VS402844: Collection with name {0} does not exist.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MissingCloseInlineMessage()
        {
            const string Format = @"Missing close expression for inline content.";
            return Format;
        }

        public static string MissingEndingBracesMessage(object arg0)
        {
            const string Format = @"No ending braces for expression '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string NestedInlinePartialsMessage()
        {
            const string Format = @"An inline partial cannot contain another inline partial";
            return Format;
        }

        public static string GetServiceArgumentError(object arg0)
        {
            const string Format = @"TF400776: '{0}' must be a non-abstract class with a public parameterless or default constructor in order to use it as parameter 'T' in GetService<T>().";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ExtensibleServiceTypeNotRegistered(object arg0)
        {
            const string Format = @"The service type '{0}' does not have a registered implementation or default implementation attribute.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ExtensibleServiceTypeNotValid(object arg0, object arg1)
        {
            const string Format = @"'{1}' does not extend or implement the service type '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ServerDataProviderNotFound(object arg0)
        {
            const string Format = @"The server data provider for service owner {0} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ClientCertificateMissing(object arg0)
        {
            const string Format = @"No certificate capable of client authentication was found in the certificate store with thumbprint {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SmartCardMissing(object arg0)
        {
            const string Format = @"The smart card containing the private key for the certificate with thumbprint {0} is not available.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ClientCertificateNoPermission(object arg0)
        {
            const string Format = @"The certificate with thumbprint {0} could not be used for client authentication. The current user may not have permission to use the certificate.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ClientCertificateErrorReadingStore(object arg0)
        {
            const string Format = @"An exception occurred while loading client authentication certificates from the certificate store: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string CannotAuthenticateAsAnotherUser(object arg0, object arg1)
        {
            const string Format = @"We were unable to establish the connection because it is configured for user {0} but you attempted to connect using user {1}. To connect as a different user perform a switch user operation. To connect with the configured identity just attempt the last operation again.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MustacheTemplateInvalidPartialReference(object arg0)
        {
            const string Format = @"Invalid partial reference: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string CannotGetUnattributedClient(object arg0)
        {
            const string Format = @"The current VssConnection does not support calling GetClient for this client type: '{0}'. Instead, use the GetClient overload which accepts a serviceIdentifier parameter to specify the intended target service for the given client.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UnknownEntityType(object arg0)
        {
            const string Format = @"Unknown entityType {0}. Cannot parse.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string GraphGroupMissingRequiredFields()
        {
            const string Format = @"Must have exactly one of originId, principlaName or displayName set.";
            return Format;
        }

        public static string GraphUserMissingRequiredFields()
        {
            const string Format = @"Must have exactly one of originId or principlaName set.";
            return Format;
        }

        public static string MustacheEvaluationResultLengthExceeded(object arg0)
        {
            const string Format = @"The maximum evaluation result length has been exceeded. The maximum allowed length is {0:N0} characters.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MustacheTemplateInlinePartialsNotAllowed()
        {
            const string Format = @"Inline partial expressions are not allowed";
            return Format;
        }

        public static string MustacheTemplateMaxDepthExceeded(object arg0)
        {
            const string Format = @"The maximum expression depth has been exceeded. The maximum allowed expression depth is {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UnexpectedTokenType()
        {
            const string Format = @"Unexpected token type. Only JObject, JArrays, Guid, String and Boolean are supported.";
            return Format;
        }

        public static string ApiVersionOutOfRange(object arg0, object arg1)
        {
            const string Format = @"The requested REST API version of {0} is out of range for this server. The latest REST API version this server supports is {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ApiVersionOutOfRangeForRoute(object arg0, object arg1)
        {
            const string Format = @"The request matched route {1}, but the requested REST API version {0} was outside the valid version range for this route.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ApiVersionOutOfRangeForRoutes(object arg0, object arg1)
        {
            const string Format = @"The following routes matched, but the requested REST API version {0} was outside the valid version ranges: {1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string UnsafeCrossOriginRequest(object arg0)
        {
            const string Format = @"A cross-origin request from origin ""{0}"" is not allowed when using cookie-based authentication. An authentication token needs to be provided in the Authorization header of the request.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
