using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.VisualStudio.Services.Client
{
    internal static class VssAadSettings
    {
        public const string DefaultAadInstance = "https://login.microsoftonline.com/";

        public const string CommonTenant = "common";

        // VSTS service principal.
        public const string Resource = "499b84ac-1321-427f-aa17-267ca6975798";

        // Visual Studio IDE client ID originally provisioned by Azure Tools.
        public const string Client = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        // AAD Production Application tenant.
        private const string ApplicationTenantId = "f8cdef31-a31e-4b4a-93e4-5f571e91255a";

#if !NETSTANDARD
        public static Uri NativeClientRedirectUri
        {
            get
            {
                Uri nativeClientRedirect = null;

                try
                {
                    string nativeRedirect = VssClientEnvironment.GetSharedConnectedUserValue<string>(VssConnectionParameterOverrideKeys.AadNativeClientRedirect);
                    if (!string.IsNullOrEmpty(nativeRedirect))
                    {
                        Uri.TryCreate(nativeRedirect, UriKind.RelativeOrAbsolute, out nativeClientRedirect);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(string.Format("NativeClientRedirectUri: {0}", e));
                }

                return nativeClientRedirect ?? new Uri("urn:ietf:wg:oauth:2.0:oob");
            }
        }

        public static string ClientId
        {
            get
            {
                string nativeRedirect = VssClientEnvironment.GetSharedConnectedUserValue<string>(VssConnectionParameterOverrideKeys.AadNativeClientIdentifier);
                return nativeRedirect ?? VssAadSettings.Client;
            }
        }
#endif

        public static string AadInstance
        {
            get
            {
#if !NETSTANDARD
                string aadInstance = VssClientEnvironment.GetSharedConnectedUserValue<string>(VssConnectionParameterOverrideKeys.AadInstance);
#else
                string aadInstance = null;
#endif

                if (string.IsNullOrWhiteSpace(aadInstance))
                {
                    aadInstance = DefaultAadInstance;
                }
                else if (!aadInstance.EndsWith("/"))
                {
                    aadInstance = aadInstance + "/";
                }

                return aadInstance;
            }
        }

#if !NETSTANDARD
        /// <summary>
        /// Application tenant either from a registry override or a constant
        /// </summary>
        public static string ApplicationTenant => 
            VssClientEnvironment.GetSharedConnectedUserValue<string>(VssConnectionParameterOverrideKeys.AadApplicationTenant) 
            ?? VssAadSettings.ApplicationTenantId;
#endif
    }
}
