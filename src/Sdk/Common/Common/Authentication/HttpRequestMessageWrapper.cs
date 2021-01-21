using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace GitHub.Services.Common
{
    internal struct HttpRequestMessageWrapper : IHttpRequest, IHttpHeaders
    {
        public HttpRequestMessageWrapper(HttpRequestMessage request)
        {
            m_request = request;
        }

        public IHttpHeaders Headers
        {
            get
            {
                return this;
            }
        }

        public Uri RequestUri
        {
            get
            {
                return m_request.RequestUri;
            }
        }

        public IDictionary<string, object> Properties
        {
            get
            {
                return m_request.Properties;
            }
        }

        IEnumerable<String> IHttpHeaders.GetValues(String name)
        {
            IEnumerable<String> values;
            if (!m_request.Headers.TryGetValues(name, out values))
            {
                values = Enumerable.Empty<String>();
            }
            return values;
        }

        void IHttpHeaders.SetValue(
            String name,
            String value)
        {
            m_request.Headers.Remove(name);
            m_request.Headers.Add(name, value);
        }

        Boolean IHttpHeaders.TryGetValues(
            String name,
            out IEnumerable<String> values)
        {
            return m_request.Headers.TryGetValues(name, out values);
        }

        private readonly HttpRequestMessage m_request;
    }
}
