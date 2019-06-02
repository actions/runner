using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.DelegatedAuthorization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.WebPlatform
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "SessionTokenException", "Microsoft.VisualStudio.Services.WebPlatform.SessionTokenException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SessionTokenException : VssServiceException
    {
        public SessionTokenException(SessionTokenError error)
            : base(Microsoft.VisualStudio.Services.WebApi.WebPlatformResources.SessionTokenException(error))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AppSessionTokenException", "Microsoft.VisualStudio.Services.WebPlatform.AppSessionTokenException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AppSessionTokenException : VssServiceException
    {
        public AppSessionTokenException(AppSessionTokenError error)
            : base(Microsoft.VisualStudio.Services.WebApi.WebPlatformResources.AppSessionTokenException(error))
        {
        }
    }
}