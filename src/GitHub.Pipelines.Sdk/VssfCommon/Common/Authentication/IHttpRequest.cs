using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Common
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
