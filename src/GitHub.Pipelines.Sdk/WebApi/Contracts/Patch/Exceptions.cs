using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.WebApi.Patch
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "PatchOperationFailedException", "Microsoft.VisualStudio.Services.WebApi.Patch.PatchOperationFailedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class PatchOperationFailedException : VssServiceException
    {
        public PatchOperationFailedException()
        {
        }

        public PatchOperationFailedException(string message)
            : base(message)
        {
        }

        public PatchOperationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PatchOperationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidPatchFieldNameException", "Microsoft.VisualStudio.Services.WebApi.Patch.InvalidPatchFieldNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidPatchFieldNameException : PatchOperationFailedException
    {
        public InvalidPatchFieldNameException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TestPatchOperationFailedException", "Microsoft.VisualStudio.Services.WebApi.Patch.TestPatchOperationFailedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TestPatchOperationFailedException : PatchOperationFailedException
    {
        public TestPatchOperationFailedException()
        {
        }

        public TestPatchOperationFailedException(string message)
            : base(message)
        {
        }

        public TestPatchOperationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TestPatchOperationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
