using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.HostAcquisition
{
    [Serializable]
    public class HostAcquisitionException : VssServiceException
    {
        public HostAcquisitionException()
        { }

        public HostAcquisitionException(string message)
            : base(message)
        { }

        public HostAcquisitionException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected HostAcquisitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class RegionNotAvailableException : HostAcquisitionException
    {
        public RegionNotAvailableException()
        {
        }

        public RegionNotAvailableException(string message)
            : base(message)
        {
        }

        public RegionNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RegionNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class HostAcquisitionBadRequestException : VssServiceException
    {
        public HostAcquisitionBadRequestException()
        {
        }

        public HostAcquisitionBadRequestException(string message)
            : base(message)
        {
        }

        public HostAcquisitionBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected HostAcquisitionBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
