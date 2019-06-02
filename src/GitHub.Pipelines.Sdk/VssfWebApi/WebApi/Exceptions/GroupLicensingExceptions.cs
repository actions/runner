using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.GroupLicensingRule
{

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidGroupLicensingOperation", "Microsoft.VisualStudio.Services.Licensing.InvalidGroupLicensingOperation, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadGroupLicensingRequestException : VssServiceException
    {
        public BadGroupLicensingRequestException()
            : base(string.Empty)
        { }

        public BadGroupLicensingRequestException(string message)
            : base(message)
        { }

        public BadGroupLicensingRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
