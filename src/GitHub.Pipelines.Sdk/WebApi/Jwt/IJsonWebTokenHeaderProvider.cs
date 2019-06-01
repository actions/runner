using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.WebApi.Jwt
{
    internal interface IJsonWebTokenHeaderProvider
    {
        void SetHeaders(IDictionary<String, Object> headers);
    }
}
