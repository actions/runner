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
}
