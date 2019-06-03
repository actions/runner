using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace GitHub.Services.Common
{
    internal struct HttpResponseMessageWrapper : IHttpResponse, IHttpHeaders
    {
        public HttpResponseMessageWrapper(HttpResponseMessage response)
        {
            m_response = response;
        }

        public IHttpHeaders Headers
        {
            get
            {
                return this;
            }
        }

        public HttpStatusCode StatusCode
        {
            get
            {
                return m_response.StatusCode;
            }
        }

        IEnumerable<String> IHttpHeaders.GetValues(String name)
        {
            IEnumerable<String> values;
            if (!m_response.Headers.TryGetValues(name, out values))
            {
                values = Enumerable.Empty<String>();
            }
            return values;
        }

        void IHttpHeaders.SetValue(
            String name,
            String value)
        {
            throw new NotSupportedException();
        }

        Boolean IHttpHeaders.TryGetValues(
            String name,
            out IEnumerable<String> values)
        {
            return m_response.Headers.TryGetValues(name, out values);
        }

        private readonly HttpResponseMessage m_response;
    }
}
