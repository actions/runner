using System;
using System.Collections.Generic;

namespace GitHub.Services.Common
{
    public interface IHttpHeaders
    {
        IEnumerable<String> GetValues(String name);

        void SetValue(String name, String value);

        Boolean TryGetValues(String name, out IEnumerable<String> values);
    }
}
