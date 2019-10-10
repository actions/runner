using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi.Patch
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "PatchOperationFailedException", "GitHub.Services.WebApi.Patch.PatchOperationFailedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "InvalidPatchFieldNameException", "GitHub.Services.WebApi.Patch.InvalidPatchFieldNameException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidPatchFieldNameException : PatchOperationFailedException
    {
        public InvalidPatchFieldNameException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TestPatchOperationFailedException", "GitHub.Services.WebApi.Patch.TestPatchOperationFailedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
