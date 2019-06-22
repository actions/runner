using System;
using System.Runtime.Serialization;
using GitHub.Services.BlobStore.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    [Serializable]
    public class DedupNotFoundException : BlobServiceException
    {
        public DedupNotFoundException(String message)
            : base(message)
        {
        }

        public DedupNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DedupNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static DedupNotFoundException Create(string dedupId)
        {
            return new DedupNotFoundException(MakeMessage(dedupId));
        }

        public static DedupNotFoundException Create(DedupIdentifier dedupId)
        {
            return DedupNotFoundException.Create(dedupId.ValueString);
        }

        private static string MakeMessage(string identifier)
        {
            return string.Format(BlobStoreResources.DedupNotFoundException(identifier));
        }
    }
}
