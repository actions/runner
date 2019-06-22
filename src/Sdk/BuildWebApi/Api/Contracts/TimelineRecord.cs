using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an entry in a build's timeline.
    /// </summary>
    [DataContract]
    public sealed class TimelineRecord : BaseSecuredObject
    {
        public TimelineRecord()
        {
        }

        internal TimelineRecord(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the record.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the record's parent.
        /// </summary>
        [DataMember]
        public Guid? ParentId
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the record.
        /// </summary>
        [DataMember(Name = "Type")]
        public String RecordType
        {
            get;
            set;
        }

        /// <summary>
        /// The name.
        /// </summary>
        [DataMember]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The start time.
        /// </summary>
        [DataMember]
        public DateTime? StartTime
        {
            get;
            set;
        }

        /// <summary>
        /// The finish time.
        /// </summary>
        [DataMember]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        /// <summary>
        /// A string that indicates the current operation.
        /// </summary>
        [DataMember]
        public String CurrentOperation
        {
            get;
            set;
        }

        /// <summary>
        /// The current completion percentage.
        /// </summary>
        [DataMember]
        public Int32? PercentComplete
        {
            get;
            set;
        }

        /// <summary>
        /// The state of the record.
        /// </summary>
        [DataMember]
        public TimelineRecordState? State
        {
            get;
            set;
        }

        /// <summary>
        /// The result.
        /// </summary>
        [DataMember]
        public TaskResult? Result
        {
            get;
            set;
        }

        /// <summary>
        /// The result code.
        /// </summary>
        [DataMember]
        public String ResultCode
        {
            get;
            set;
        }

        /// <summary>
        /// The change ID.
        /// </summary>
        /// <remarks>
        /// This is a monotonically-increasing number used to ensure consistency in the UI.
        /// </remarks>
        [DataMember]
        public Int32 ChangeId
        {
            get;
            set;
        }

        /// <summary>
        /// The time the record was last modified.
        /// </summary>
        [DataMember]
        public DateTime LastModified
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the agent running the operation.
        /// </summary>
        [DataMember]
        public String WorkerName
        {
            get;
            set;
        }

        /// <summary>
        /// An ordinal value relative to other records.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? Order
        {
            get;
            set;
        }

        /// <summary>
        /// A reference to a sub-timeline.
        /// </summary>
        [DataMember]
        public TimelineReference Details
        {
            get;
            set;
        }

        /// <summary>
        /// The number of errors produced by this operation.
        /// </summary>
        [DataMember]
        public Int32? ErrorCount
        {
            get;
            set;
        }

        /// <summary>
        /// The number of warnings produced by this operation.
        /// </summary>
        [DataMember]
        public Int32? WarningCount
        {
            get;
            set;
        }

        /// <summary>
        /// The list of issues produced by this operation.
        /// </summary>
        public List<Issue> Issues
        {
            get
            {
                if (m_issues == null)
                {
                    m_issues = new List<Issue>();
                }
                return m_issues;
            }
        }

        /// <summary>
        /// The REST URL of the timeline record.
        /// </summary>
        [DataMember]
        public Uri Url
        {
            get;
            set;
        }

        /// <summary>
        /// A reference to the log produced by this operation.
        /// </summary>
        [DataMember]
        public BuildLogReference Log
        {
            get;
            set;
        }

        /// <summary>
        /// A reference to the task represented by this timeline record.
        /// </summary>
        [DataMember]
        public TaskReference Task
        {
            get;
            set;
        }

        /// <summary>
        /// Attempt number of record.
        /// </summary>
        [DataMember]
        public Int32 Attempt
        {
            get;
            set;
        }

        /// <summary>
        /// String identifier that is consistent across attempts.
        /// </summary>
        [DataMember]
        public String Identifier
        {
            get;
            set;
        }

        public IList<TimelineAttempt> PreviousAttempts
        {
            get
            {
                if (m_previousAttempts == null)
                {
                    m_previousAttempts = new List<TimelineAttempt>();
                }
                return m_previousAttempts;
            }
        }


        /// <summary>
        /// The links to other objects related to this object.
        /// </summary>
        public ReferenceLinks Links
        {
            get
            {
                if (m_links == null)
                {
                    m_links = new ReferenceLinks();
                }
                return m_links;
            }
        }

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        private ReferenceLinks m_links;

        [DataMember(Name = "Issues", EmitDefaultValue = false, Order = 60)]
        private List<Issue> m_issues;

        [DataMember(Name = "PreviousAttempts", EmitDefaultValue = false)]
        private List<TimelineAttempt> m_previousAttempts;
    }
}
