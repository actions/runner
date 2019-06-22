using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [ServiceEventObject]
    public class BuildsDeletedEvent : BuildsDeletedEvent1
    {
    }

    // trying this out to avoid future compat issues.
    // the idea is to keep this around when we create BuildsDeletedEvent2, and make BuildsDeletedEvent inherit from BuildsDeletedEvent2 instead of BuildsDeletedEvent1
    // then, when we publish to service bus, we can send BuildsDeletedEvent1 explicitly along with BuildsDeletedEvent
    [DataContract]
    [ServiceEventObject]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BuildsDeletedEvent1
    {
        /// <summary>
        /// The ID of the project.
        /// </summary>
        [DataMember]
        public Guid ProjectId
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the definition.
        /// </summary>
        [DataMember]
        public Int32 DefinitionId
        {
            get;
            set;
        }

        /// <summary>
        /// The IDs of the builds that were deleted.
        /// </summary>
        public List<Int32> BuildIds
        {
            get
            {
                if (m_buildIds == null)
                {
                    m_buildIds = new List<Int32>();
                }

                return m_buildIds;
            }
        }

        [DataMember(Name = "BuildIds")]
        private List<Int32> m_buildIds;
    }
}
