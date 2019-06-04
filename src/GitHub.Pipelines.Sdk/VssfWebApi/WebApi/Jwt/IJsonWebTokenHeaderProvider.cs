using System;
using System.Collections.Generic;

namespace GitHub.Services.WebApi.Jwt
{
    internal interface IJsonWebTokenHeaderProvider
    {
        void SetHeaders(IDictionary<String, Object> headers);
    }
}
