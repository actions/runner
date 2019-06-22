using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a timeline.
    /// </summary>
    [DataContract]
    public class TimelineReference : BaseSecuredObject
    {
        internal TimelineReference()
        {
        }

        internal TimelineReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the timeline.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id
        {
            get;
            internal set;
        }

        /// <summary>
        /// The change ID.
        /// </summary>
        /// <remarks>
        /// This is a monotonically-increasing number used to ensure consistency in the UI.
        /// </remarks>
        [DataMember(Order = 2)]
        public Int32 ChangeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// The REST URL of the timeline.
        /// </summary>
        [DataMember(Order = 3)]
        public String Url
        {
            get;
            set;
        }
    }
}
