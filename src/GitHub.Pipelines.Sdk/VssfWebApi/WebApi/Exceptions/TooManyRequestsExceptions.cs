using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Security.Permissions;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi.Exceptions
{
    /// <summary>
    /// Throw when there is any throttling happening
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class TooManyRequestsException : VssServiceException
    {
        public DateTime RetryAfterDateTime { get; set; }

        public TooManyRequestsException(string message, DateTime retryAfterDateTime)
            : base(message)
        {
            this.RetryAfterDateTime = retryAfterDateTime;
        }

        public TooManyRequestsException(string message, DateTime retryAfterDateTime, Exception innerException)
            : base(message, innerException)
        {
            this.RetryAfterDateTime = retryAfterDateTime;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected TooManyRequestsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.RetryAfterDateTime = info.GetDateTime("RetryAfterDateTime");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("RetryAfterDateTime", this.RetryAfterDateTime);

            base.GetObjectData(info, context);
        }
    }

    /// <summary>
    /// Throw when there is any throttling happening in the AAD Layer.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class AadThrottlingException : TooManyRequestsException
    {
        public AadThrottlingException(string message, DateTime retryAfterDateTime)
            : base(message, retryAfterDateTime)
        {
        }

        public AadThrottlingException(string message, DateTime retryAfterDateTime, Exception innerException)
            : base(message, retryAfterDateTime, innerException)
        {
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected AadThrottlingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Throw when there is any throttling happening in the VssClient Layer.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class ClientRequestThrottledException : TooManyRequestsException
    {
        public ClientRequestThrottledException(string message, DateTime retryAfterDateTime)
            : base(message, retryAfterDateTime)
        {
        }

        public ClientRequestThrottledException(string message, DateTime retryAfterDateTime, Exception innerException)
            : base(message, retryAfterDateTime, innerException)
        {
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected ClientRequestThrottledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
