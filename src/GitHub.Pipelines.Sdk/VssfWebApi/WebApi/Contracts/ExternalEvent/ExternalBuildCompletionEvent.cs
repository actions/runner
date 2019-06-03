using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    public class ExternalBuildCompletionEvent
    {
        /// <summary>
        /// Current resource version.
        /// </summary>
        [IgnoreDataMember]
        public static ApiResourceVersion CurrentVersion = new ApiResourceVersion(new Version(1, 0), 1);

        /// <summary>
        /// This string identifies the publisher that received the message.
        /// </summary>
        [DataMember]
        public String PublisherId;

        /// <summary>
        /// This string identifies the external source that sent the message.
        /// </summary>
        [DataMember]
        public String SourceId;

        /// <summary>
        /// Identifer of the build on the external system. External-system specific.
        /// </summary>
        [DataMember]
        public String Id;

        /// <summary>
        /// Name of the build on the external system. External-system specific.
        /// </summary>
        [DataMember]
        public String Name;

        /// <summary>
        /// The Status of the build (see BuildStatus)
        /// </summary>
        [DataMember]
        public ExternalBuildStatus Status;

        /// <summary>
        /// The Duration of the build
        /// </summary>
        [DataMember]
        public TimeSpan Duration;

        /// <summary>
        /// When the build started
        /// </summary>
        [DataMember]
        public DateTime StartTime;

        /// <summary>
        /// Who started the build
        /// </summary>
        [DataMember]
        public String StartedBy;

        /// <summary>
        /// Json blob containing the details of the external build
        /// </summary>
        [DataMember]
        public String Details;

        [DataMember]
        public String PipelineEventId { get; set; }

        /// <summary>
        /// Property bag.  Subscription publisher inputs are copied here.
        /// </summary>
        [DataMember]
        public IDictionary<String, String> Properties;
    }

    [DataContract]
    public enum ExternalBuildStatus
    {
        /// <summary>
        /// Unknown status.
        /// </summary>
        [EnumMember]
        Unknown = 0,

        /// <summary>
        /// The build is currently in progress.
        /// </summary>
        [EnumMember]
        InProgress = 1,

        /// <summary>
        /// The build has completed and succeeded.
        /// </summary>
        [EnumMember]
        Succeeded = 2,

        /// <summary>
        /// The build has completed and failed.
        /// </summary>
        [EnumMember]
        Failed = 3,

        /// <summary>
        /// The build was cancelled.
        /// </summary>
        [EnumMember]
        Canceled = 4,

        /// <summary>
        /// The build has not yet started.
        /// </summary>
        [EnumMember]
        NotStarted = 5,
    }


}
