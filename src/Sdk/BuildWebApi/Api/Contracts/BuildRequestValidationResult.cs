using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the result of validating a build request.
    /// </summary>
    [DataContract]
    public class BuildRequestValidationResult : BaseSecuredObject
    {
        public BuildRequestValidationResult()
        {
        }

        public BuildRequestValidationResult(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The result.
        /// </summary>
        [DataMember]
        public ValidationResult Result
        {
            get;
            set;
        }

        /// <summary>
        /// The message associated with the result.
        /// </summary>
        [DataMember]
        public String Message
        {
            get;
            set;
        }
    }
}
