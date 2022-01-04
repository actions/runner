using System;
using System.Collections.Generic;

namespace GitHub.Services.Common
{
    public interface IHttpRequest
    {
        IHttpHeaders Headers
        {
            get;
        }

        Uri RequestUri
        {
            get;
        }

        IDictionary<string, object> Properties
        {
            get;
        }
    }
}
