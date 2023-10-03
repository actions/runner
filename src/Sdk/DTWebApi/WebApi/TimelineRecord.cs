using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TimelineRecord
    {
        public TimelineRecord()
            : this(null)
        {
        }

        private TimelineRecord(TimelineRecord recordToBeCloned)
        {
            this.EnsureInitialized();

            if (recordToBeCloned != null)
            {
                this.Attempt = recordToBeCloned.Attempt;
                this.ChangeId = recordToBeCloned.ChangeId;
                this.CurrentOperation = recordToBeCloned.CurrentOperation;
                this.FinishTime = recordToBeCloned.FinishTime;
                this.Id = recordToBeCloned.Id;
                this.Identifier = recordToBeCloned.Identifier;
                this.LastModified = recordToBeCloned.LastModified;
                this.Location = recordToBeCloned.Location;
                this.Name = recordToBeCloned.Name;
                this.Order = recordToBeCloned.Order;
                this.ParentId = recordToBeCloned.ParentId;
                this.PercentComplete = recordToBeCloned.PercentComplete;
                this.RecordType = recordToBeCloned.RecordType;
                this.Result = recordToBeCloned.Result;
                this.ResultCode = recordToBeCloned.ResultCode;
                this.StartTime = recordToBeCloned.StartTime;
                this.State = recordToBeCloned.State;
                this.TimelineId = recordToBeCloned.TimelineId;
                this.WorkerName = recordToBeCloned.WorkerName;
                this.RefName = recordToBeCloned.RefName;
                this.ErrorCount = recordToBeCloned.ErrorCount;
                this.WarningCount = recordToBeCloned.WarningCount;
                this.NoticeCount = recordToBeCloned.NoticeCount;
                this.AgentPlatform = recordToBeCloned.AgentPlatform;

                if (recordToBeCloned.Log != null)
                {
                    this.Log = new TaskLogReference
                    {
                        Id = recordToBeCloned.Log.Id,
                        Location = recordToBeCloned.Log.Location,
                    };
                }

                if (recordToBeCloned.Details != null)
                {
                    this.Details = new TimelineReference
                    {
                        ChangeId = recordToBeCloned.Details.ChangeId,
                        Id = recordToBeCloned.Details.Id,
                        Location = recordToBeCloned.Details.Location,
                    };
                }

                if (recordToBeCloned.m_issues?.Count > 0)
                {
                    this.Issues.AddRange(recordToBeCloned.Issues.Select(i => i.Clone()));
                }

                if (recordToBeCloned.m_previousAttempts?.Count > 0)
                {
                    this.m_previousAttempts.AddRange(recordToBeCloned.m_previousAttempts);
                }

                if (recordToBeCloned.m_variables?.Count > 0)
                {
                    // Don't pave over the case-insensitive Dictionary we initialized above.
                    foreach (var kvp in recordToBeCloned.m_variables)
                    {
                        m_variables[kvp.Key] = kvp.Value.Clone();
                    }
                }
            }
        }

        [DataMember(Order = 1)]
        public Guid Id
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Guid? TimelineId
        {
            get;
            set;
        }

        [DataMember(Order = 2)]
        public Guid? ParentId
        {
            get;
            set;
        }

        [DataMember(Name = "Type", Order = 3)]
        public string RecordType
        {
            get;
            set;
        }

        [DataMember(Order = 4)]
        public string Name
        {
            get;
            set;
        }

        [DataMember(Order = 5)]
        public DateTime? StartTime
        {
            get;
            set;
        }

        [DataMember(Order = 6)]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        [DataMember(Order = 7)]
        public string CurrentOperation
        {
            get;
            set;
        }

        [DataMember(Order = 8)]
        public Int32? PercentComplete
        {
            get;
            set;
        }

        [DataMember(Order = 9)]
        public TimelineRecordState? State
        {
            get;
            set;
        }

        [DataMember(Order = 10)]
        public TaskResult? Result
        {
            get;
            set;
        }

        [DataMember(Order = 11)]
        public string ResultCode
        {
            get;
            set;
        }

        [DataMember(Order = 12)]
        public Int32 ChangeId
        {
            get;
            set;
        }

        [DataMember(Order = 13)]
        public DateTime LastModified
        {
            get;
            set;
        }

        [DataMember(Order = 14)]
        public string WorkerName
        {
            get;
            set;
        }

        [DataMember(Order = 15, EmitDefaultValue = false)]
        public Int32? Order
        {
            get;
            set;
        }

        [DataMember(Order = 16, EmitDefaultValue = false)]
        public string RefName
        {
            get;
            set;
        }

        [DataMember(Order = 20)]
        public TaskLogReference Log
        {
            get;
            set;
        }

        [DataMember(Order = 30)]
        public TimelineReference Details
        {
            get;
            set;
        }

        public Int32 ErrorCount
        {
            get
            {
                return m_errorCount.GetValueOrDefault(0);
            }
            set
            {
                m_errorCount = value;
            }
        }

        public Int32 WarningCount
        {
            get
            {
                return m_warningCount.GetValueOrDefault(0);
            }
            set
            {
                m_warningCount = value;
            }
        }

        public Int32 NoticeCount
        {
            get
            {
                return m_noticeCount.GetValueOrDefault(0);
            }
            set
            {
                m_noticeCount = value;
            }
        }

        public List<Issue> Issues
        {
            get
            {
                return m_issues;
            }
        }

        [DataMember(Order = 100)]
        public Uri Location
        {
            get;
            set;
        }

        [DataMember(Order = 130)]
        public Int32 Attempt
        {
            get;
            set;
        }

        [DataMember(Order = 131)]
        public string Identifier
        {
            get;
            set;
        }

        [DataMember(Order = 132, EmitDefaultValue = false)]
        public string AgentPlatform
        {
            get;
            set;
        }

        public IList<TimelineAttempt> PreviousAttempts
        {
            get
            {
                return m_previousAttempts;
            }
        }

        public IDictionary<string, VariableValue> Variables
        {
            get
            {
                return m_variables;
            }
        }

        public TimelineRecord Clone()
        {
            return new TimelineRecord(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.EnsureInitialized();
        }


        /// <summary>
        /// DataContractSerializer bypasses all constructor logic and inline initialization!
        /// This method takes the place of a workhorse constructor for baseline initialization.
        /// The expectation is for this logic to be accessible to constructors and also to the OnDeserialized helper.
        /// </summary>
        private void EnsureInitialized()
        {
            // Note that ?? is a short-circuiting operator.  (??= would be preferable, but it's not supported in the .NET Framework version currently used by actions/runner.)

            // De-nullify the following historically-nullable ints.
            // (After several weeks in production, it may be possible to eliminate these nullable backing fields.)
            m_errorCount = m_errorCount ?? 0;
            m_warningCount = m_warningCount ?? 0;
            m_noticeCount = m_noticeCount ?? 0;

            m_issues = m_issues ?? new List<Issue>();
            m_previousAttempts = m_previousAttempts ?? new List<TimelineAttempt>();
            this.Attempt = Math.Max(this.Attempt, 1);

            // Ensure whatever content may have been deserialized for m_variables is backed by a case-insensitive Dictionary.
            var empty = Enumerable.Empty<KeyValuePair<string, VariableValue>>();
            m_variables = new Dictionary<string, VariableValue>(m_variables ?? empty, StringComparer.OrdinalIgnoreCase);
        }

        [DataMember(Name = nameof(ErrorCount), Order = 40)]
        private Int32? m_errorCount;

        [DataMember(Name = nameof(WarningCount), Order = 50)]
        private Int32? m_warningCount;

        [DataMember(Name = nameof(NoticeCount), Order = 55)]
        private Int32? m_noticeCount;

        [DataMember(Name = nameof(Issues), EmitDefaultValue = false, Order = 60)]
        private List<Issue> m_issues;

        [DataMember(Name = nameof(Variables), EmitDefaultValue = false, Order = 80)]
        private Dictionary<string, VariableValue> m_variables;

        [DataMember(Name = nameof(PreviousAttempts), EmitDefaultValue = false, Order = 120)]
        private List<TimelineAttempt> m_previousAttempts;
    }
}
