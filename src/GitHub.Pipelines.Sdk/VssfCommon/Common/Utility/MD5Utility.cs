using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.VisualStudio.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class MD5Utility
    {
        /// <summary>
        /// Creates an MD5 crypto service provider. Returns null if FIPS algorithm policy is enabled.
        /// </summary>
        /// <returns>An MD5 provider or null if FIPS prevents using it.</returns>
        [SuppressMessage("Microsoft.Cryptographic.Standard","CA5350:MD5CannotBeUsed", Justification="legacy code")]
        public static MD5 TryCreateMD5Provider()
        {
            // if HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa\FipsAlgorithmPolicy\Enabled is set to 1 then the following is true
            // a) new MD5CryptoServiceProvider() throws System.InvalidOperationException
            // b) MD5.Create() throws System.Reflection.TargetInvocationException

            // .NET runtime reads this value once on a first create of MD5CryptoServiceProvider. 
            // If FIPS algorithm policy was enabled and someone disabled it, .NET processes have to be restarted to be able create MD5 providers.
            MD5 md5 = null;

            if (s_fipsAlgorithmPolicyEnabled == c_disabled || s_fipsAlgorithmPolicyEnabled == c_unknown)
            {
                // Allow the use of an environment variable to turn on FIPS compliance (for testing)
                if (null != Environment.GetEnvironmentVariable("DD_SUITES_FIPS"))
                {
                    s_fipsAlgorithmPolicyEnabled = c_enabled;
                }
                else
                {
                    try
                    {
                        md5 = new MD5CryptoServiceProvider();
                        s_fipsAlgorithmPolicyEnabled = c_disabled;
                    }
                    catch (InvalidOperationException)
                    {
                        s_fipsAlgorithmPolicyEnabled = c_enabled;
                    }
                }
            }
 
            return md5;
        }

 
        // Controled by HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa\FipsAlgorithmPolicy\Enabled registry true.
        // It is not possible to create MD5 crypto service provider, if FIPS (Federal Information Processing Standard) algorithm policy is enabled.
        // Values:
        //  -1 - unknown
        //   0 - policy disabled, MD5 crypto provider can be created
        //   1 - policy enabled, MD5 crypto provider can NOT be created
        private static Int32 s_fipsAlgorithmPolicyEnabled = c_unknown;
        private const Int32 c_enabled = 1;
        private const Int32 c_disabled = 0;
        private const Int32 c_unknown = -1;

    }
}
