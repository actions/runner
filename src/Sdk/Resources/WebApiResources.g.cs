using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class WebApiResources
    {
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
            const string Format = @"You cannot add location mappings to service definitions that are not part of the FullyQualified type.";
            return Format;
        }

        public static string InvalidAccessMappingLocationServiceUrl()
        {
            const string Format = @"The access mapping is not valid and cannot be registered. The location service URL cannot be null or empty.";
            return Format;
        }

        public static string ServiceDefinitionDoesNotExist(object arg0, object arg1)
        {
            const string Format = @"The service definition with service type '{0}' and identifier '{1}' does not exist.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ServiceDefinitionWithNoLocations(object arg0)
        {
            const string Format = @"The service with the following type does not have a location mapping: {0}. You must provide at least one location in order to configure locations for an external service.";
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

        public static string GetServiceArgumentError(object arg0)
        {
            const string Format = @"'{0}' must be a non-abstract class with a public parameterless or default constructor in order to use it as parameter 'T' in GetService<T>().";
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
