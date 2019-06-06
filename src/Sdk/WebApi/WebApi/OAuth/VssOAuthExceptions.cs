using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides a base class for all OAuth exceptions.
    /// </summary>
    [Serializable]
    public class VssOAuthException : VssServiceException
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthException</c> instance with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public VssOAuthException(String message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthException</c> instance with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">An object that describes the error that caused the current exception</param>
        public VssOAuthException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthException</c> instance with serialized data.
        /// </summary>
        /// <param name="info">The <c>SerializationInfo</c> that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The <c>StreamingContext</c> that contains contextual information about the source or destination</param>
        protected VssOAuthException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Thrown when an exception is encountered processing an OAuth 2.0 token request.
    /// </summary>
    [Serializable]
    public class VssOAuthTokenRequestException : VssOAuthException
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequestException</c> instance with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public VssOAuthTokenRequestException(String message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequestException</c> instance with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">An object that describes the error that caused the current exception</param>
        public VssOAuthTokenRequestException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthException</c> instance with serialized data.
        /// </summary>
        /// <param name="info">The <c>SerializationInfo</c> that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The <c>StreamingContext</c> that contains contextual information about the source or destination</param>
        protected VssOAuthTokenRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.Error = info.GetString("m_error");
        }

        /// <summary>
        /// Gets or sets the OAuth 2.0 error code. See <see cref="VssOAuthErrorCodes"/> for potential values.
        /// </summary>
        public String Error
        {
            get;
            set;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("m_error", this.Error);
        }
    }
}
