using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi.Exceptions
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "MissingRequiredParameterException", "GitHub.Services.WebApi.Exceptions.MissingRequiredParameterException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MissingRequiredParameterException : VssServiceException
    {
        public MissingRequiredParameterException()
        {
        }

        public MissingRequiredParameterException(string message)
            : base(message)
        {
        }

        public MissingRequiredParameterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MissingRequiredParameterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MissingRequiredHeaderException : VssServiceException
    {
        public MissingRequiredHeaderException()
        {
        }

        public MissingRequiredHeaderException(string message)
            : base(message)
        {
        }

        public MissingRequiredHeaderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MissingRequiredHeaderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MultipleHeaderValuesException : VssServiceException
    {
        public MultipleHeaderValuesException()
        {
        }

        public MultipleHeaderValuesException(string message)
            : base(message)
        {
        }

        public MultipleHeaderValuesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MultipleHeaderValuesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
