using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.ContentValidation.Client
{
    [DataContract]
    public class ContentViolationReport
    {
        /// <summary>
        /// Id of the account to report which might get taken down after the validation process (i.e. collection)
        /// </summary>
        [DataMember]
        public Guid HostId { get; set; }

        /// <summary>
        /// Id of the source container (like project in TFS case).
        /// </summary>
        [DataMember]
        public Guid ContainerId { get; set; }

        /// <summary>
        /// Url of the content to be reported to Content Validation Service (AVERT).
        /// </summary>
        [DataMember]
        public String ContentUrl { get; set; }

        /// <summary>
        /// Category of the content violation.
        /// </summary>
        [DataMember]
        public ContentViolationCategory ViolationCategory { get; set; }

        /// <summary>
        /// Additional details about the offensive content.
        /// </summary>
        [DataMember]
        public String AdditionalDetails { get; set; }
    }

    [DataContract]
    public class ContentViolationReportResult : ISecuredObject
    {
        /// <summary>
        /// Id of the result coming from Avert.
        /// </summary>
        [DataMember]
        public Guid? Id { get; set; }

        /// <summary>
        /// Status of the report.
        /// </summary>
        [DataMember]
        public ContentViolationReportResultStatus Status { get; set; }

        Guid ISecuredObject.NamespaceId => ContentValidationSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => ContentValidationSecurityConstants.Write;

        string ISecuredObject.GetToken() => ContentValidationSecurityConstants.ViolationsToken;
    }

    public enum ContentViolationReportResultStatus
    {
        InProgress,
        Succeeded,
        Unset
    }

    public enum ContentViolationCategory
    {
        /// <summary>
        /// No category.
        /// </summary>
        None = 7,

        /// <summary>
        /// Spam or phishing.
        /// </summary>
        Spam = 4,

        /// <summary>
        /// Contains nudity of pornography.
        /// </summary>
        Nudity = 3,

        /// <summary>
        /// Harassment or threatening.
        /// </summary>
        Harassment = 1,

        /// <summary>
        /// Child endangerment or exploitation.
        /// </summary>
        Child = 2,

        /// <summary>
        /// Other
        /// </summary>
        Other = 6
    }
}
