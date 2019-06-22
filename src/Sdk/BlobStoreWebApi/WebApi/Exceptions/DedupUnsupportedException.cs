using System;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi.Exceptions
{
    /// <summary>
    /// Indicates that a dedup operation is currently unsupported
    /// </summary>
    public class DedupUnsupportedException : Exception
    {
        public DedupUnsupportedException(string message) : base(message)
        {
        }

        public DedupUnsupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DedupUnsupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
