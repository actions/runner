using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace GitHub.Services.Identity
{
    public class RequestHeadersContext
    {
        internal SequenceContext SequenceContext { get; set; }

        internal bool IgnoreCache { get; set; }

        public RequestHeadersContext(SequenceContext sequenceContext) :
            this(sequenceContext, false)
        {
        }

        public RequestHeadersContext(SequenceContext sequenceContext, bool ignoreCache)
        {
            SequenceContext = sequenceContext;
            IgnoreCache = ignoreCache;
        }

        private static bool ParseOrGetDefault(string s)
        {
            if (!string.IsNullOrWhiteSpace(s) && bool.TryParse(s, out var value))
            {
                return value;
            }
            return false;
        }

        internal class HeadersUtils
        {
            public static KeyValuePair<string, string>[] PopulateRequestHeaders(RequestHeadersContext requestHeaderContext)
            {
                if (requestHeaderContext == null)
                {
                    return new KeyValuePair<string, string>[0];
                }

                KeyValuePair<string, string>[] sequenceContextHeaders = SequenceContext.HeadersUtils.PopulateRequestHeaders(requestHeaderContext.SequenceContext);
                KeyValuePair<string, string>[] resultHeaderPairs = new KeyValuePair<string, string>[sequenceContextHeaders.Length + 1];
                sequenceContextHeaders.CopyTo(resultHeaderPairs, 0);
                resultHeaderPairs[sequenceContextHeaders.Length] = new KeyValuePair<string, string>(c_ignoreCacheHeader, requestHeaderContext.IgnoreCache.ToString());
                return resultHeaderPairs;
            }

            public static bool TryExtractRequestHeaderContext(HttpRequestHeaders httpRequestHeaders, out RequestHeadersContext requestHeadersContext)
            {
                requestHeadersContext = null;
                bool hasIgnoreCacheHeader = httpRequestHeaders.TryGetValues(c_ignoreCacheHeader, out IEnumerable<string> ignoreCacheValue) && ignoreCacheValue != null;
                bool hasSequenceContextHeader =  SequenceContext.HeadersUtils.TryExtractSequenceContext(httpRequestHeaders, out SequenceContext sequenceContext); 
                bool ignoreCache = ParseOrGetDefault(ignoreCacheValue?.FirstOrDefault());
                requestHeadersContext = new RequestHeadersContext(sequenceContext, ignoreCache);
                return hasIgnoreCacheHeader || hasSequenceContextHeader;
            }

            private const string c_ignoreCacheHeader = "X-VSSF-IMS-IgnoreCache";
        }

    }
}
