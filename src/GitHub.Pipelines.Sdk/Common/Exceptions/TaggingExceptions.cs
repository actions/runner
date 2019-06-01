using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Common
{
    /// <summary>
    /// Thrown when a tag definition cannot be found.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TagNotFoundException", "Microsoft.VisualStudio.Services.Common.TagNotFoundException, Microsoft.VisualStudio.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TagNotFoundException : VssServiceException
    {
        public TagNotFoundException()
        { }

        public TagNotFoundException(string message)
            : base(message)
        { }

        public TagNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected TagNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }


    /// <summary>
    /// Thrown when an operation on a tag failed because of client-supplied values.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TagOperationFailed", "Microsoft.VisualStudio.Services.Common.TagOperationFailed, Microsoft.VisualStudio.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TagOperationFailed : VssServiceException
    {
        public TagOperationFailed()
        { }

        public TagOperationFailed(string message)
            : base(message)
        { }

        public TagOperationFailed(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected TagOperationFailed(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
