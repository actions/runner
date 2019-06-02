using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Common.Internal;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.Services.WebApi.Utilities.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class UserAgentUtility
    {
        private static Lazy<List<ProductInfoHeaderValue>> s_defaultRestUserAgent 
            = new Lazy<List<ProductInfoHeaderValue>>(ConstructDefaultRestUserAgent);

        public static List<ProductInfoHeaderValue> GetDefaultRestUserAgent()
        {
            return s_defaultRestUserAgent.Value;
        }

        private static List<ProductInfoHeaderValue> ConstructDefaultRestUserAgent()
        {
#if !NETSTANDARD
            // Get just the exe file name without the path.
            String exe;
            try
            {
                exe = Path.GetFileName(NativeMethods.GetModuleFileName());
            }
            catch (Exception e)
            {
                Trace.WriteLine("DefaultUserAgent: Unable to get exe.  " + e.ToString());

                // We weren't allowed to get the exe file name, so we go on.
                exe = "unavailable";
            }

            Tuple<string, int> skuInfo = null;
            if (String.Equals(exe, "devenv.exe", StringComparison.OrdinalIgnoreCase))
            {
                skuInfo = GetCurrentSkuInfo();
            }

            String app = String.Empty;
            if (AppDomain.CurrentDomain != null)
            {
                app = (String)AppDomain.CurrentDomain.GetData(AdminConstants.ApplicationName);
            }

            if (!String.IsNullOrEmpty(app))
            {
                exe = String.Concat(exe, "[", app, "]");
            }
#endif
            // Pick up the assembly version from this dll
            String fileVersion = "unavailable";
            try
            {
                AssemblyFileVersionAttribute attr = typeof(UserAgentUtility).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
                if (attr != null)
                {
                    fileVersion = attr.Version;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("DefaultUserAgent: Unable to get fileVersion: " + e.ToString());
            }


#if !NETSTANDARD

            Debug.Assert(fileVersion.StartsWith("16", StringComparison.OrdinalIgnoreCase),
                            "The SKU numbers here are only meant to work with Dev16. For later versions the SKU numbers mapped in the GetSkuNumber method need to be updated.");
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            builder.Append(exe);
            if (skuInfo != null)
            {
                builder.Append(", ");
                builder.Append(skuInfo.Item1);
                builder.Append(", SKU:");
                builder.Append(skuInfo.Item2.ToString(CultureInfo.InvariantCulture));
            }
            builder.Append(")");

            String commentValue = builder.ToString();
#else
            String commentValue = string.Format("(NetStandard; {0})", RuntimeInformation.OSDescription.Replace('(', '[').Replace(')', ']').Trim());
#endif
            return new List<ProductInfoHeaderValue> {
                new ProductInfoHeaderValue("VSServices", fileVersion),
                new ProductInfoHeaderValue(commentValue) };
        }


#if !NETSTANDARD
        private static Lazy<string> s_defaultSoapUserAgent = new Lazy<string>(ConstructDefaultSoapUserAgent);

        public static String GetDefaultSoapUserAgent()
        {
            return s_defaultSoapUserAgent.Value;
        }

        private static string ConstructDefaultSoapUserAgent()
        {
            // Get just the exe file name without the path.
            String exe;
            try
            {
                exe = Path.GetFileName(NativeMethods.GetModuleFileName());
            }
            catch (Exception e)
            {
                Trace.WriteLine("DefaultUserAgent: Unable to get exe: " + e.ToString());

                // We weren't allowed to get the exe file name, so we go on.
                exe = "unavailable";
            }

            Tuple<string, int> skuInfo = null;
            if (String.Equals(exe, "devenv.exe", StringComparison.OrdinalIgnoreCase))
            {
                skuInfo = GetCurrentSkuInfo();
            }

            String app = String.Empty;
            if (AppDomain.CurrentDomain != null)
            {
                app = (String)AppDomain.CurrentDomain.GetData("ApplicationName");
            }

            if (!String.IsNullOrEmpty(app))
            {
                exe = String.Concat(exe, "[", app, "]");
            }

            // Pick up the assembly version from the current dll.
            String fileVersion = String.Empty;
            try
            {
                Object[] attrs = typeof(UserAgentUtility).Assembly.GetCustomAttributes(false);
                foreach (Object attr in attrs)
                {
                    if (attr is AssemblyFileVersionAttribute)
                    {
                        fileVersion = ((AssemblyFileVersionAttribute)attr).Version;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("DefaultUserAgent: Unable to get fileVersion: " + e.ToString());

                // We weren't allowed to get the version info, so we go on.
                fileVersion = "unavailable";
            }

            StringBuilder userAgent = new StringBuilder();
            userAgent.Append("Team Foundation (");
            userAgent.Append(exe);
            userAgent.Append(", ");
            userAgent.Append(fileVersion);
            if (skuInfo != null)
            {
                userAgent.Append(", ");
                userAgent.Append(skuInfo.Item1);
                userAgent.Append(", SKU:");
                userAgent.Append(skuInfo.Item2.ToString(CultureInfo.InvariantCulture));
            }
            userAgent.Append(")");
            return userAgent.ToString();
        }


        private static Tuple<string, int> GetCurrentSkuInfo()
        {
            string vsSkuEdition = Environment.GetEnvironmentVariable("VSSKUEDITION");
            if (!string.IsNullOrEmpty(vsSkuEdition))
            {
                Tuple<string, int> skuInfo;
                if (s_dev16SkuToAgentStringMap.TryGetValue(vsSkuEdition, out skuInfo))
                {
                    return skuInfo;
                }
                else
                {
                    Debug.Fail("Unrecognized value for VSSKUEDITION: '{0}'.  This value needs to be added to the s_dev16SkuToAgentStringMap.", vsSkuEdition);
                }
            }

            return new Tuple<string, int>(ClientSkuNames.Dev16.Other, ClientSkuNumbers.Dev16Other);
        }

        /// <summary>
        /// The key is the SKU name provided by VSSKUEDITION env variable. The value is a tuple. Item1 is a string for the SKU Name to put in the User Agent string, and Item2 is an int for the SkuCode.
        /// </summary>
        private static readonly Dictionary<string, Tuple<string, int>> s_dev16SkuToAgentStringMap = new Dictionary<string, Tuple<string, int>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Enterprise", new Tuple<string, int>(ClientSkuNames.Dev16.Enterprise, ClientSkuNumbers.Dev16Enterprise) },
            { "Professional", new Tuple<string, int>(ClientSkuNames.Dev16.Pro, ClientSkuNumbers.Dev16Pro) },
            { "Community", new Tuple<string, int>(ClientSkuNames.Dev16.Community, ClientSkuNumbers.Dev16Community) },
            { "V3|UNKNOWN", new Tuple<string, int>(ClientSkuNames.Dev16.TE, ClientSkuNumbers.Dev16TeamExplorer)  },
            { "V4|UNKNOWN", new Tuple<string, int>(ClientSkuNames.Dev16.Sql, ClientSkuNumbers.Dev16Sql) },  // future release as of 4/25/2017.
            { "IntShell", new Tuple<string, int>(ClientSkuNames.Dev16.IntShell, ClientSkuNumbers.Dev16IntShell) }  // future release as of 4/25/2017.  This key may change.
        };
#endif
    }
}
