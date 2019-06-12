using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "VssServiceResponseException", "GitHub.Services.WebApi.VssServiceResponseException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssServiceResponseException : VssServiceException
    {
        public VssServiceResponseException(HttpStatusCode code, String message, Exception innerException)
            : base(message, innerException)
        {
            this.HttpStatusCode = code;
        }

        protected VssServiceResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        {
            HttpStatusCode = (HttpStatusCode)info.GetInt32("HttpStatusCode");
        }

        [SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("HttpStatusCode", (int)HttpStatusCode);
        }

        public HttpStatusCode HttpStatusCode { get; private set; }
    }
}
