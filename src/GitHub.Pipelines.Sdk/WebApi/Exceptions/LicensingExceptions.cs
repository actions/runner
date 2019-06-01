using Microsoft.VisualStudio.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Services.Licensing
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "LicensingException", "Microsoft.VisualStudio.Services.Licensing.LicensingException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class LicensingException : VssServiceException
    {
        public LicensingException(string message)
            : base(message)
        { }

        public LicensingException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [ExceptionMapping("0.0", "3.0", "LicenseServiceUnavailableException", "Microsoft.VisualStudio.Services.Licensing.InvalidRightNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]

    public class LicenseServiceUnavailableException : VssServiceException
    {
        public LicenseServiceUnavailableException()
        {
        }

        public LicenseServiceUnavailableException(string message)
            : base(message)
        {
        }

        public LicenseServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidRightNameException", "Microsoft.VisualStudio.Services.Licensing.InvalidRightNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidRightNameException : LicensingException
    {
        public InvalidRightNameException()
            : base(string.Empty)
        { }

        public InvalidRightNameException(string message)
            : base(message)
        { }

        public InvalidRightNameException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidClientVersionException", "Microsoft.VisualStudio.Services.Licensing.InvalidClientVersionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidClientVersionException : LicensingException
    {
        public InvalidClientVersionException()
            : base(string.Empty)
        { }

        public InvalidClientVersionException(string message)
            : base(message)
        { }

        public InvalidClientVersionException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidClientRightsQueryContextException", "Microsoft.VisualStudio.Services.Licensing.InvalidClientRightsQueryContextException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidClientRightsQueryContextException : LicensingException
    {
        public InvalidClientRightsQueryContextException()
            : base(string.Empty)
        { }

        public InvalidClientRightsQueryContextException(string message)
            : base(message)
        { }

        public InvalidClientRightsQueryContextException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidVisualStudioOffersQueryContextException", "Microsoft.VisualStudio.Services.Licensing.InvalidVisualStudioOffersQueryContextException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidVisualStudioOffersQueryContextException : LicensingException
    {
        public InvalidVisualStudioOffersQueryContextException()
            : base(string.Empty)
        { }

        public InvalidVisualStudioOffersQueryContextException(string message)
            : base(message)
        { }

        public InvalidVisualStudioOffersQueryContextException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "LicenseNotAvailableException", "Microsoft.VisualStudio.Services.Licensing.LicenseNotAvailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class LicenseNotAvailableException : LicensingException
    {
        public LicenseNotAvailableException()
            : base(string.Empty)
        { }

        public LicenseNotAvailableException(string message)
            : base(message)
        { }

        public LicenseNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidExtensionIdException", "Microsoft.VisualStudio.Services.Licensing.InvalidExtensionIdException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidExtensionIdException : LicensingException
    {
        public InvalidExtensionIdException()
            : base(string.Empty)
        { }

        public InvalidExtensionIdException(string message)
            : base(message)
        { }

        public InvalidExtensionIdException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidLicensingOperation", "Microsoft.VisualStudio.Services.Licensing.InvalidLicensingOperation, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidLicensingOperation : LicensingException
    {
        public InvalidLicensingOperation()
            : base(string.Empty)
        { }

        public InvalidLicensingOperation(string message)
            : base(message)
        { }

        public InvalidLicensingOperation(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TransferUserLicenseException", "Microsoft.VisualStudio.Services.Licensing.InvalidLicensingOperation, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TransferUserLicenseException : LicensingException
    {
        public TransferUserLicenseException()
            : base(string.Empty)
        { }

        public TransferUserLicenseException(string message)
            : base(message)
        { }

        public TransferUserLicenseException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "MaterializationFailedDuringLicensingException", "Microsoft.VisualStudio.Services.Licensing.MaterializationFailedDuringLicensingException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MaterializationFailedDuringLicensingException : LicensingException
    {
        public MaterializationFailedDuringLicensingException()
            : base(string.Empty)
        { }

        public MaterializationFailedDuringLicensingException(string message)
            : base(message)
        { }

        public MaterializationFailedDuringLicensingException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
