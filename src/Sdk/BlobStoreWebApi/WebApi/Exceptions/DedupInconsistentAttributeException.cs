using System;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi
{
    [Serializable]
    public class DedupInconsistentAttributeException : BlobServiceException
    {
        public DedupInconsistentAttributeException(String message)
            : base(message)
        {
        }
        
        protected DedupInconsistentAttributeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static DedupInconsistentAttributeException Create(string dedupId)
        {
            return new DedupInconsistentAttributeException(MakeMessage(dedupId));
        }

        private static string MakeMessage(string identifier)
        {
            return string.Format(BlobStoreResources.DedupInconsistentAttributeException(identifier));
        }
    }
}
