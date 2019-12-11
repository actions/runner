using GitHub.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace GitHub.Services.WebApi
{
    internal static class HttpMessageExtensions
    {
        private const string tracerKey = "VSS_HTTP_TIMER_TRACE";

        internal static void Trace(this HttpRequestMessage request)
        {
            Object tracerObj = null;
            VssRequestTimerTrace tracer = null;
            if (request.Properties.TryGetValue(tracerKey, out tracerObj))
            {
                tracer = tracerObj as VssRequestTimerTrace;
                Debug.Assert(tracer != null, "Tracer object is the wrong type!");
            }
            else
            {
                tracer = new VssRequestTimerTrace();
                request.Properties[tracerKey] = tracer;
            }

            if (tracer != null)
            {
                tracer.TraceRequest(request);
            }
        }

        internal static void Trace(this HttpResponseMessage response)
        {
            Object tracerObj = null;
            VssRequestTimerTrace tracer = null;
            if (response.RequestMessage.Properties.TryGetValue(tracerKey, out tracerObj))
            {
                tracer = tracerObj as VssRequestTimerTrace;
                Debug.Assert(tracer != null, "Tracer object is the wrong type!");
            }

            if (tracer != null)
            {
                tracer.TraceResponse(response);
            }
        }
    }

    // a little class to trace perf of web requests
    // does nothing without TRACE set
    internal class VssRequestTimerTrace
    {
        internal VssRequestTimerTrace()
        {
#if TRACE
            _requestTimer = new Stopwatch();
#endif
        }
        internal void TraceRequest(HttpRequestMessage message)
        {
#if TRACE
            string requestString = message.GetRequestString();

            VssPerformanceEventSource.Log.RESTStart(Guid.Empty, requestString);
            _requestTimer.Start();

#endif
        }
        internal void TraceResponse(HttpResponseMessage response)
        {
#if TRACE
            _requestTimer.Stop();
            String responseString = response.GetResponseString(_requestTimer.ElapsedMilliseconds);
#endif
        }
#if TRACE
        private Stopwatch _requestTimer;
#endif
    }

#if TRACE
    internal static class VssRequestLoggingExtensions
    {
        internal static String GetRequestString(this HttpRequestMessage message)
        {
            String verb, area, resource;
            Guid vssE2EId;

            TryGetHeaderGuid(message.Headers, Common.Internal.HttpHeaders.VssE2EID, out vssE2EId);

            ExtractRequestStrings(message, out verb, out resource, out area);

            return String.Format(CultureInfo.InvariantCulture, _requestFormat, message.RequestUri.AbsoluteUri, verb, resource, area, vssE2EId);
        }

        internal static String GetResponseString(this HttpResponseMessage response, long milliseconds)
        {
            String verb, area, resource;
            Guid activityId = Guid.Empty, vssE2EId = Guid.Empty;

            ExtractRequestStrings(response.RequestMessage, out verb, out resource, out area);

            TryGetHeaderGuid(response.Headers, Common.Internal.HttpHeaders.VssE2EID, out vssE2EId);
            TryGetHeaderGuid(response.Headers, Common.Internal.HttpHeaders.ActivityId, out activityId);

            return String.Format(CultureInfo.InvariantCulture, _responseFormat, response.RequestMessage.RequestUri.AbsoluteUri, verb, resource, area, vssE2EId, activityId, milliseconds);
        }

        private static void ExtractRequestStrings(HttpRequestMessage message, out String verb, out String resource, out String area)
        {
            verb = message.Method.ToString().ToUpper();
            resource = _unknown;
            area = _unknown;

            int segments = message.RequestUri.Segments.Length;

            if (segments > 0)
            {
                //if we did our REST APIs right the resource had better be the last
                //segment.
                resource = message.RequestUri.Segments[segments - 1].TrimEnd('/');
            }

            for (int i = 0; i < segments; i++)
            {
                //area should be the first segment after _apis
                //some resources don't have an area, so it will be the same
                //which is OK, we'll know what it means :)
                if (String.Compare(message.RequestUri.Segments[i], _apis, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (segments > (i + 1))
                    {
                        area = message.RequestUri.Segments[i + 1].TrimEnd('/');
                    }
                    break;
                }
            }
        }

        private static bool TryGetHeaderGuid(HttpHeaders headers, string key, out Guid value)
        {
            IEnumerable<String> values;
            value = Guid.Empty;
            if (headers.TryGetValues(key, out values))
            {
                return Guid.TryParse(values.FirstOrDefault(), out value);
            }

            return false;
        }

        //[URI] (VERB)RESOURCE[AREA] E2EId: E2EId
        private const String _requestFormat = "Web method running: [{0}] ({1}){2}[{3}] E2EId: {4}";
        //[URI] (VERB)RESOURCE[AREA] E2EId: E2EId, ActivityId: ActivityId N ms
        private const String _responseFormat = "Web method response: [{0}] ({1}){2}[{3}] E2EId: {4}, ActivityId: {5} {6} ms";
        private const String _unknown = "<unknown>";
        private const String _apis = "_apis/";
    }
#endif
}
