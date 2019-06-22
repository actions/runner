using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    [Serializable]
    public class BlobUploadException : VssServiceException
    {
        public BlobUploadException(String message)
            : base(message)
        {
        }

        public BlobUploadException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public BlobUploadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
