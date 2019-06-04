using System.Net;

namespace GitHub.Services.Common
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
