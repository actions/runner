using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Zeus
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BlobCopyRequestNotFoundException", "Microsoft.VisualStudio.Services.Zeus.BlobCopyRequestNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BlobCopyRequestNotFoundException : VssServiceException
    {
        public BlobCopyRequestNotFoundException(int requestId)
            : base(ZeusWebApiResources.BlobCopyRequestNotFoundException(requestId))
        {
        }

        public BlobCopyRequestNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BlobCopyRequestNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DuplicateBlobCopyRequestException", "Microsoft.VisualStudio.Services.Zeus.DuplicateBlobCopyRequestException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DuplicateBlobCopyRequestException : VssServiceException
    {
        public DuplicateBlobCopyRequestException (int requestId)
            : base(ZeusWebApiResources.BlobCopyRequestNotFoundException(requestId))
        {
        }

        public DuplicateBlobCopyRequestException (String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DuplicateBlobCopyRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
