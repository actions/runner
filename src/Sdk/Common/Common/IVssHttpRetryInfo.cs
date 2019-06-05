using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    public interface IVssHttpRetryInfo
    {
        void InitialAttempt(HttpRequestMessage request);

        void Retry(TimeSpan sleep);

        void Reset();
    }
}
