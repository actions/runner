using System;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi.Exceptions
{
    /// <summary>
    /// Indicates that a local directory (e.g. source or target directory) is invalid as it does not exist or is not a directory
    /// </summary>
    [Obsolete("This Exception is Obselete, use InvalidPathException")]
    public class InvalidLocalDirectoryException : Exception
    {
        public InvalidLocalDirectoryException(string message) : base(message)
        {
        }

        public InvalidLocalDirectoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidLocalDirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
