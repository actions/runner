using System.Runtime.Serialization;

namespace GitHub.Services.Operations
{
    /// <summary>
    /// The status of an operation.
    /// </summary>
    [DataContract]
    public enum OperationStatus
    {
        /// <summary>
        /// The operation does not have a status set.
        /// </summary>
        [EnumMember]
        NotSet = 0,

        /// <summary>
        /// The operation has been queued.
        /// </summary>
        [EnumMember]
        Queued,

        /// <summary>
        /// The operation is in progress.
        /// </summary>
        [EnumMember]
        InProgress,

        /// <summary>
        /// The operation was cancelled by the user.
        /// </summary>
        [EnumMember]
        Cancelled,

        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        [EnumMember]
        Succeeded,

        /// <summary>
        /// The operation completed with a failure.
        /// </summary>
        [EnumMember]
        Failed
    };
}
