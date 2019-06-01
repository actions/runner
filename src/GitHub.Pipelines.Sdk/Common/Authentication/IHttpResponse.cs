using System.Net;

namespace Microsoft.VisualStudio.Services.Common
{
    public interface IHttpResponse
    {
        IHttpHeaders Headers
        {
            get;
        }

        HttpStatusCode StatusCode
        {
            get;
        }
    }
}
