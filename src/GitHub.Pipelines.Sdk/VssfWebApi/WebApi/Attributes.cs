using System;
using GitHub.Services.WebApi.Internal;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// This attribute provides the location service area identifier in order to target the location service
    /// instance which has the service definitions for the HTTP resources in the specified service area.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ResourceAreaAttribute : Attribute
    {
        public ResourceAreaAttribute(String areaId)
        {
            this.AreaId = new Guid(areaId);
        }

        public readonly Guid AreaId;
    }

    /// <summary>
    /// Use in conjunction with JsonCompatConverter.  This attribute describes a model property or field change at a particular API version.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class CompatPropertyAttribute : Attribute
    {
        /// <summary>
        /// This attribute describes a model property or field change at a particular API version.
        /// </summary>
        /// <param name="oldName">Old name of the serialized property.</param>
        /// <param name="majorApiVersion">The major version component of the max version of the api to support the old property name.</param>
        /// <param name="minorApiVersion">The minor version component of the max version of the api to support the old property name.</param>
        public CompatPropertyAttribute(String oldName, Int32 majorApiVersion, Int32 minorApiVersion = 0)
        {
            OldName = oldName;
            MaxApiVersion = new Version(majorApiVersion, minorApiVersion);
        }

        /// <summary>
        /// Old name of the serialized property.
        /// </summary>
        public String OldName { get; private set; }

        /// <summary>
        /// The max version of the api to support the old property name.
        /// </summary>
        public Version MaxApiVersion { get; private set; }
    }

    /// <summary>
    /// This tells the client generator to set this property to the content of the repsonse 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ClientResponseContentAttribute : Attribute
    {
        public ClientResponseContentAttribute()
        {
        }
    }

    /// <summary>
    /// This tells the client generator to set this property to the header value from the response.  This should only be added to types of IEnumerable&lt;String&gt;
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ClientResponseHeaderAttribute : Attribute
    {
        public ClientResponseHeaderAttribute(string headerName)
        {
            HeaderName = headerName;
        }

        public string HeaderName { get; private set; }
    }

    /// <summary>
    /// Tells the client generator to create meta data for this model, even if it is not referenced directly or indirectly from the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class ClientIncludeModelAttribute : Attribute
    {
        public ClientIncludeModelAttribute()
        {
            Languages = RestClientLanguages.All;
        }

        public ClientIncludeModelAttribute(RestClientLanguages languages)
        {
            Languages = languages;
        }

        public RestClientLanguages Languages { get; }
    }

    /// <summary>
    /// Marks a class, method or property for internal use only.  This attribute ensures the item
    /// does not show up in public documentation,  adds EditorBrowsableState.Never in C# clients
    /// to hide the item, and optionaly adds @internal in TypeScript clients which removes the
    /// item from the TypeScript declare (d.ts) file.  This does not exempt this API from the
    /// formal REST Api review process.  Our internal APIs must meet the same standards and
    /// guidelines as our public APIs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ClientInternalUseOnlyAttribute : Attribute
    {
        /// <summary>
        /// Marks a class, method or property for internal use only.  This attribute ensures the item
        /// does not show up in public documentation,  adds EditorBrowsableState.Never in C# clients
        /// to hide the item, and optionaly adds @internal in TypeScript clients which removes the
        /// item from the TypeScript declare (d.ts) file.  This does not exempt this API from the
        /// formal REST Api review process.  Our internal APIs must meet the same standards and
        /// guidelines as our public APIs.
        /// </summary>
        /// <param name="omitFromTypeScriptDeclareFile">Default is true.  Set to false if you need the item to appear in the TypeScript declare (d.ts) file for use by extensions.</param>
        public ClientInternalUseOnlyAttribute(bool omitFromTypeScriptDeclareFile = true)
        {
            OmitFromTypeScriptDeclareFile = omitFromTypeScriptDeclareFile;
        }

        /// <summary>
        /// Set to false if you need the item to appear in the TypeScript declare (d.ts) file for use by extensions.
        /// </summary>
        public bool OmitFromTypeScriptDeclareFile { get;  set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ClientCircuitBreakerSettingsAttribute : Attribute
    {
        public ClientCircuitBreakerSettingsAttribute(int timeoutSeconds, int failurePercentage)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            ErrorPercentage = failurePercentage;
        }

        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public TimeSpan Timeout { get; private set; }

        /// <summary>
        /// Percentage of failed commands
        /// </summary>
        public int ErrorPercentage { get; private set; }

        /// <summary>
        /// Number of max concurrent requests
        /// </summary>
        public int MaxConcurrentRequests { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ClientCancellationTimeoutAttribute : Attribute
    {
        public ClientCancellationTimeoutAttribute(int timeoutSeconds)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        /// <summary>
        /// Timeout in seconds for request cancellation
        /// </summary>
        public TimeSpan Timeout { get; private set; }
    }

    /// <summary>
    /// Indicates which headers are considered to contain sensitive information by a particular HttpClient.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ClientSensitiveHeaderAttribute : Attribute
    {
        public string HeaderName { get; set; }

        public ClientSensitiveHeaderAttribute(string headerName)
        {
            HeaderName = headerName;
        }
    }
}
