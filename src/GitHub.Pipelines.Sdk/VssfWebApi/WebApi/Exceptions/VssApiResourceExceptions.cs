using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Exception thrown when the requested API resource location was not found on the server
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssResourceNotFoundException", "Microsoft.VisualStudio.Services.WebApi.VssResourceNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssResourceNotFoundException : VssServiceException
    {
        public VssResourceNotFoundException(Guid locationId)
            : this(WebApiResources.ResourceNotRegisteredMessage(locationId))
        {
        }

        public VssResourceNotFoundException(Guid locationId, Uri serverBaseUri)
            : this(WebApiResources.ResourceNotFoundOnServerMessage(locationId, serverBaseUri))
        {
        }

        public VssResourceNotFoundException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Base exception class for api resource version exceptions
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssResourceVersionException", "Microsoft.VisualStudio.Services.WebApi.VssResourceVersionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class VssResourceVersionException : VssServiceException
    {
        public VssResourceVersionException(String message)
            : base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssInvalidApiResourceVersionException", "Microsoft.VisualStudio.Services.WebApi.VssInvalidApiResourceVersionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssInvalidApiResourceVersionException : VssResourceVersionException
    {
        public VssInvalidApiResourceVersionException(String apiResourceVersionString)
            : base(WebApiResources.InvalidApiVersionStringMessage(apiResourceVersionString))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssApiResourceDuplicateIdException", "Microsoft.VisualStudio.Services.WebApi.VssApiResourceDuplicateIdException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssApiResourceDuplicateIdException: VssApiRouteRegistrationException
    {
        public VssApiResourceDuplicateIdException(Guid locationId)
            : base(WebApiResources.ApiResourceDuplicateIdMessage(locationId))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssApiResourceDuplicateRouteNameException", "Microsoft.VisualStudio.Services.WebApi.VssApiResourceDuplicateRouteNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssApiResourceDuplicateRouteNameException : VssApiRouteRegistrationException
    {
        public VssApiResourceDuplicateRouteNameException(string routeName)
            : base(WebApiResources.ApiResourceDuplicateRouteNameMessage(routeName))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssApiRouteRegistrationException", "Microsoft.VisualStudio.Services.WebApi.VssApiRouteRegistrationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class VssApiRouteRegistrationException : VssResourceVersionException
    {
        public VssApiRouteRegistrationException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the requested version of a resource is not supported on the server
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssVersionNotSupportedException", "Microsoft.VisualStudio.Services.WebApi.VssVersionNotSupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssVersionNotSupportedException : VssResourceVersionException
    {
        public VssVersionNotSupportedException(ApiResourceLocation location, Version requestedVersion, Version minSupportedVersion, Uri serverBaseUri)
            : base(WebApiResources.ClientResourceVersionNotSupported(location.Area + ":" + location.ResourceName + " " + location.Id, requestedVersion, serverBaseUri, minSupportedVersion))
        {
        }
    }

    /// <summary>
    /// Exception thrown when the requested version of a resource is greater than the latest api version the server supports.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class VssVersionOutOfRangeException : VssResourceVersionException
    {
        public VssVersionOutOfRangeException(Version requestedVersion, Version maxSupportedVersion)
            : base(WebApiResources.ApiVersionOutOfRange(requestedVersion, maxSupportedVersion))
        {
        }

        public VssVersionOutOfRangeException(ApiResourceVersion requestedApiVersion, string routeMatchedExceptVersion)
            : base(WebApiResources.ApiVersionOutOfRangeForRoute(requestedApiVersion, routeMatchedExceptVersion))
        {
        }

        public VssVersionOutOfRangeException(ApiResourceVersion requestedApiVersion, IEnumerable<string> routesMatchedExceptVersion)
            : base(WebApiResources.ApiVersionOutOfRangeForRoutes(requestedApiVersion, string.Join(", ", routesMatchedExceptVersion)))
        {
        }

        public VssVersionOutOfRangeException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the api-version is not supplied for a particular type of request
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssVersionNotSpecifiedException", "Microsoft.VisualStudio.Services.WebApi.VssVersionNotSpecifiedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssVersionNotSpecifiedException : VssResourceVersionException
    {
        public VssVersionNotSpecifiedException(String httpMethod)
            : base(WebApiResources.VersionNotSuppliedMessage(httpMethod))
        {
        }
    }

    /// <summary>
    /// Exception thrown when the requested version of a resource is a "preview" api, but -preview is not supplied in the request's api-version
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssInvalidPreviewVersionException", "Microsoft.VisualStudio.Services.WebApi.VssInvalidPreviewVersionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssInvalidPreviewVersionException : VssResourceVersionException
    {
        public VssInvalidPreviewVersionException(ApiResourceVersion requestedVersion)
            : base(WebApiResources.PreviewVersionNotSuppliedMessage(requestedVersion.ToString()))
        {
        }
    }

    /// <summary>
    /// Exception thrown when a request body's contentType is not supported by a given controller.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssRequestContentTypeNotSupportedException", "Microsoft.VisualStudio.Services.WebApi.VssRequestContentTypeNotSupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssRequestContentTypeNotSupportedException : VssServiceException
    {
        public VssRequestContentTypeNotSupportedException(String contentType, String httpMethod, IEnumerable<String> validContentTypes)
            : base(WebApiResources.RequestContentTypeNotSupported(contentType, httpMethod, String.Join(", ", validContentTypes)))
        {
        }
    }

    /// <summary>
    /// Exception thrown when a cross-origin request is made using cookie-based authentication from an unsafe domain.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class VssApiUnsafeCrossOriginRequestException : VssServiceException
    {
        public VssApiUnsafeCrossOriginRequestException(String origin)
            : base(WebApiResources.UnsafeCrossOriginRequest(origin))
        {
        }
    }
}
