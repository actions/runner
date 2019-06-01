using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Directories
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DirectoryException", "Microsoft.VisualStudio.Services.Directories.DirectoryException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DirectoryException : VssServiceException
    {
        public DirectoryException() : base()
        {

        }

        public DirectoryException(string message) : base(message)
        {

        }

        public DirectoryException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public DirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DirectoryParameterException", "Microsoft.VisualStudio.Services.Directories.DirectoryParameterException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DirectoryParameterException : DirectoryException
    {
        public DirectoryParameterException() : base()
        {

        }

        public DirectoryParameterException(string message) : base(message)
        {

        }

        public DirectoryParameterException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public DirectoryParameterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }

    [Serializable]
    public class DirectoryEntityTypeException : DirectoryException
    {
        public DirectoryEntityTypeException() : base()
        {

        }

        public DirectoryEntityTypeException(string message) : base(message)
        {

        }

        public DirectoryEntityTypeException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public DirectoryEntityTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DirectoryHostTypeNotSupportedException", "Microsoft.VisualStudio.Services.Directories.DirectoryHostTypeNotSupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public class DirectoryHostTypeNotSupportedException : DirectoryException
    {
        public DirectoryHostTypeNotSupportedException() : base()
        {

        }

        public DirectoryHostTypeNotSupportedException(string message) : base(message)
        {

        }

        public DirectoryHostTypeNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public DirectoryHostTypeNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }

    [Serializable]
    public class InvalidDirectoryEntityResultException : DirectoryException
    {
        public InvalidDirectoryEntityResultException() : base()
        {

        }

        public InvalidDirectoryEntityResultException(string message) : base(message)
        {

        }

        public InvalidDirectoryEntityResultException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public InvalidDirectoryEntityResultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
