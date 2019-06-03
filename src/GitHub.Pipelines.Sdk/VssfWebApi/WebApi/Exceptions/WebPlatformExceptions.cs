using GitHub.Services.Common;
using GitHub.Services.DelegatedAuthorization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.WebPlatform
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SessionTokenException", "GitHub.Services.WebPlatform.SessionTokenException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SessionTokenException : VssServiceException
    {
        public SessionTokenException(SessionTokenError error)
            : base(GitHub.Services.WebApi.WebPlatformResources.SessionTokenException(error))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AppSessionTokenException", "GitHub.Services.WebPlatform.AppSessionTokenException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AppSessionTokenException : VssServiceException
    {
        public AppSessionTokenException(AppSessionTokenError error)
            : base(GitHub.Services.WebApi.WebPlatformResources.AppSessionTokenException(error))
        {
        }
    }
}
