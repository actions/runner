using System;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class UrlUtil
    {
        public static bool IsHosted(string serverUrl)
        {
            return serverUrl.IndexOf(".visualstudio.com", StringComparison.OrdinalIgnoreCase) != -1
                || serverUrl.IndexOf(".tfsallin.net", StringComparison.OrdinalIgnoreCase) != -1
                || serverUrl.IndexOf(".vsallin.net", StringComparison.OrdinalIgnoreCase) != -1;
        }
    }
}
