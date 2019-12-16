using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GitHub.Services.WebApi.Utilities.Internal
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

            String commentValue = string.Format("(NetStandard; {0})", RuntimeInformation.OSDescription.Replace('(', '[').Replace(')', ']').Trim());

            return new List<ProductInfoHeaderValue> {
                new ProductInfoHeaderValue("VSServices", fileVersion),
                new ProductInfoHeaderValue(commentValue) };
        }
    }
}
