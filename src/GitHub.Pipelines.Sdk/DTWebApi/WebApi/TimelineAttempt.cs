using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TimelineAttempt
    {
        /// <summary>
        /// Gets or sets the unique identifier for the record.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Identifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the attempt of the record.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Attempt
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the timeline identifier which owns the record representing this attempt.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid TimelineId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the record identifier located within the specified timeline.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid RecordId
        {
            get;
            set;
        }
    }
}
