using System;

namespace GitHub.Services.BlobStore.WebApi
{
    [Serializable]
    public class BlobNotFoundException : BlobServiceException
    {
        public BlobNotFoundException(String message)
            : base(message)
        {
        }

        public BlobNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public static BlobNotFoundException Create(string blobId)
        {
            return new BlobNotFoundException(MakeMessage(blobId));
        }

        private static string MakeMessage(string identifier)
        {
            return string.Format(BlobStoreResources.BlobNotFoundException(identifier));
        }
    }
}
