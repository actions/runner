using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Represents version information for a REST Api resource
    /// </summary>
    [DataContract]
    public class ApiResourceVersion
    {
        private const String c_PreviewStageName = "preview";

        /// <summary>
        /// Construct a new API Version info
        /// </summary>
        /// <param name="apiVersion">Public API version</param>
        /// <param name="resourceVersion">Resource version</param>
        public ApiResourceVersion(double apiVersion, int resourceVersion = 0)
            : this(new Version(apiVersion.ToString("0.0", CultureInfo.InvariantCulture)), resourceVersion)
        {
        }

        /// <summary>
        /// Construct a new API resource Version
        /// </summary>
        public ApiResourceVersion()
            : this(1.0)
        {
        }

        /// <summary>
        /// Construct a new API Version info
        /// </summary>
        /// <param name="apiVersion">Public API version</param>
        /// <param name="resourceVersion">Resource version</param>
        public ApiResourceVersion(Version apiVersion, int resourceVersion = 0)
        {
            ArgumentUtility.CheckForNull(apiVersion, "apiVersion");

            ApiVersion = apiVersion;
            ResourceVersion = resourceVersion;

            if (resourceVersion > 0)
            {
                IsPreview = true;
            }
        }

        /// <summary>
        /// Construct a new API Version info from the given version string
        /// </summary>
        /// <param name="apiResourceVersionString">Version string in the form: 
        /// {ApiMajor}.{ApiMinor}[-{stage}[.{resourceVersion}]]
        /// 
        /// For example: 1.0 or 2.0-preview or 2.0-preview.3</param>
        public ApiResourceVersion(String apiResourceVersionString)
        {
            this.FromVersionString(apiResourceVersionString);
        }

        /// <summary>
        /// Public API version. This is the version that the public sees and is used for a large
        /// group of services (e.g. the TFS 1.0 API)
        /// </summary>
        public Version ApiVersion { get; private set; }

        /// <summary>
        /// String representation of the Public API version. This is the version that the public sees and is used 
        /// for a large group of services (e.g. the TFS 1.0 API)
        /// </summary>
        [DataMember(Name = "ApiVersion")]
        public String ApiVersionString
        {
            get
            {
                return ApiVersion.ToString(2);
            }
            private set
            {
                if (String.IsNullOrEmpty(value))
                {
                    ApiVersion = new Version(1, 0);
                }
                else
                {
                    ApiVersion = new Version(value);
                }
            }
        }

        /// <summary>
        /// Internal resource version. This is defined per-resource and is used to support
        /// build-to-build compatibility of API changes within a given (in-preview) public api version.
        /// For example, within the TFS 1.0 API release cycle, while it is still in preview, a resource's
        /// data structure may be changed. This resource can be versioned such that older clients will
        /// still work (requests will be sent to the older version) and new/upgraded clients will
        /// talk to the new version of the resource.
        /// </summary>
        [DataMember]
        public int ResourceVersion { get; set; }

        /// <summary>
        /// Is the public API version in preview
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsPreview { get; set; }

        /// <summary>
        /// Returns the version string in the form:
        /// {ApiMajor}.{ApiMinor}[-{stage}[.{resourceVersion}]]
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sbVersion = new StringBuilder(ApiVersion.ToString(2));
            if (IsPreview)
            {
                sbVersion.Append('-');
                sbVersion.Append(c_PreviewStageName);

                if (ResourceVersion > 0)
                {
                    sbVersion.Append('.');
                    sbVersion.Append(ResourceVersion);
                }
            }
            return sbVersion.ToString();
        }

        private void FromVersionString(String apiVersionString)
        {
            if (String.IsNullOrEmpty(apiVersionString))
            {
                throw new VssInvalidApiResourceVersionException(apiVersionString);
            }

            // Check for a stage/resourceVersion string
            int dashIndex = apiVersionString.IndexOf('-');
            if (dashIndex >= 0)
            {
                String stageName;

                // Check for a '.' which separate stage from resource version
                int dotIndex = apiVersionString.IndexOf('.', dashIndex);
                if (dotIndex > 0)
                {
                    stageName = apiVersionString.Substring(dashIndex + 1, dotIndex - dashIndex - 1);

                    int resourceVersion;
                    String resourceVersionString = apiVersionString.Substring(dotIndex + 1);
                    if (!int.TryParse(resourceVersionString, out resourceVersion))
                    {
                        throw new VssInvalidApiResourceVersionException(apiVersionString);
                    }
                    else
                    {
                        this.ResourceVersion = resourceVersion;
                    }
                }
                else
                {
                    stageName = apiVersionString.Substring(dashIndex + 1);
                }

                // Check for supported stage names
                if (String.Equals(stageName, c_PreviewStageName, StringComparison.OrdinalIgnoreCase))
                {
                    IsPreview = true;
                }
                else
                {
                    throw new VssInvalidApiResourceVersionException(apiVersionString);
                }

                // Api version is the string before the dash
                apiVersionString = apiVersionString.Substring(0, dashIndex);
            }

            // Trim a leading "v" for version
            apiVersionString = apiVersionString.TrimStart('v');

            double apiVersionValue;
            if (!double.TryParse(apiVersionString, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out apiVersionValue))
            {
                throw new VssInvalidApiResourceVersionException(apiVersionString);
            }

            // Store the api version
            this.ApiVersion = new Version(apiVersionValue.ToString("0.0", CultureInfo.InvariantCulture));
        }
    }
}
