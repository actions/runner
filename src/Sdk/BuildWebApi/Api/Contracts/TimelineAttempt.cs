using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public sealed class TimelineAttempt : BaseSecuredObject
    {
        public TimelineAttempt()
        {
        }

        internal TimelineAttempt(
            ISecuredObject securedObject)
            : base(securedObject)
        {
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
