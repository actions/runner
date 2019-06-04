using System;
using System.ComponentModel;
using System.Net.Http;

namespace GitHub.Services.Common.Diagnostics
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class HttpRequestMessageExtensions
    {
        public static VssHttpMethod GetHttpMethod(this HttpRequestMessage message)
        {
            String methodName = message.Method.Method;
            VssHttpMethod httpMethod = VssHttpMethod.UNKNOWN;
            if (!Enum.TryParse<VssHttpMethod>(methodName, true, out httpMethod))
            {
                httpMethod = VssHttpMethod.UNKNOWN;
            }
            return httpMethod;
        }

        public static VssTraceActivity GetActivity(this HttpRequestMessage message)
        {
            Object traceActivity;
            if (!message.Properties.TryGetValue(VssTraceActivity.PropertyName, out traceActivity))
            {
                return VssTraceActivity.Empty;
            }
            return (VssTraceActivity)traceActivity;
        }
    }
}
