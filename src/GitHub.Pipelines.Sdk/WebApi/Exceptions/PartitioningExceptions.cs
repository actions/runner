using Microsoft.VisualStudio.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Partitioning
{
    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public partial class PartitionNotFoundException : VssServiceException
    {
        public PartitionNotFoundException(String message)
            : base(message)
        {
        }

        public PartitionNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected PartitionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public partial class PartitionContainerMustBeOfflineException : VssServiceException
    {
        public PartitionContainerMustBeOfflineException(String message)
            : base(message)
        {
        }

        public PartitionContainerMustBeOfflineException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public PartitionContainerMustBeOfflineException(Guid containerId)
            : base(PartitioningResources.PartitionContainerMustBeOfflineError(containerId))
        {
        }

        protected PartitionContainerMustBeOfflineException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
