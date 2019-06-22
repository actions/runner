using System;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi.Exceptions
{
    /// <summary>
    /// Indicates that a local directory (e.g. source or target directory) is invalid as it does not exist or is not a directory
    /// </summary>
    public class InvalidPathException : Exception
    {
        public InvalidPathException(string message) : base(message)
        {
        }

        public InvalidPathException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidPathException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
