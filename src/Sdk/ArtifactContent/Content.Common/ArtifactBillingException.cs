using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// BillingException - Artifact billing related exceptions.
    /// DEVNOTE: Ecommerce v2 APIs will provide template exceptions instead and then this can be removed.
    /// </summary>
    [Serializable]
    public class ArtifactBillingException : VssServiceException
    {
        public const string DefaultExceptionMessage =
            "Artifact cannot be uploaded because max quantity has been exceeded or the payment instrument is invalid. " +
            "https://aka.ms/artbilling for details.";

        /// <inheritdoc />
        /// <summary>
        /// Default const.
        /// </summary>
        public ArtifactBillingException() : base(DefaultExceptionMessage)
        {
        }

        /// <summary>
        /// Exception with message and ex.
        /// </summary>
        /// <param name="message">The message overload.</param>
        /// <param name="ex">The exception</param>
        public ArtifactBillingException(string message, Exception ex) : base(message, ex)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Exception with just the message.
        /// </summary>
        /// <param name="message">The message overload.</param>
        public ArtifactBillingException(string message) : base(message)
        {
        }

        /// <summary>
        /// BillingException with serialization info.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The context.</param>
        protected ArtifactBillingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
