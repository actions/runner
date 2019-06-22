using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    [Serializable]
    public class BlobServiceException : VssServiceException
    {
        public BlobServiceException(string message, Exception ex) : base(message, ex)
        {
        }

        public BlobServiceException(string message)
            : this(message, null)
        {
        }

        protected BlobServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
