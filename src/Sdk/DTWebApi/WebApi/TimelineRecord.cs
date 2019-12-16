using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TimelineRecord
    {
        public TimelineRecord()
        {
            this.Attempt = 1;
        }

        private TimelineRecord(TimelineRecord recordToBeCloned)
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

            if (recordToBeCloned.m_issues?.Count> 0)
            {
                this.Issues.AddRange(recordToBeCloned.Issues.Select(i => i.Clone()));
            }

            if (recordToBeCloned.m_previousAttempts?.Count > 0)
            {
                this.PreviousAttempts.AddRange(recordToBeCloned.PreviousAttempts);
            }

            if (recordToBeCloned.m_variables?.Count > 0)
            {
                this.m_variables = recordToBeCloned.Variables.ToDictionary(k => k.Key, v => v.Value.Clone());
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
        public String RecordType
        {
            get;
            set;
        }

        [DataMember(Order = 4)]
        public String Name
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
        public String CurrentOperation
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
        public String ResultCode
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
        public String WorkerName
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
        public String RefName
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

        [DataMember(Order = 40)]
        public Int32? ErrorCount
        {
            get;
            set;
        }

        [DataMember(Order = 50)]
        public Int32? WarningCount
        {
            get;
            set;
        }

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

        public IDictionary<String, VariableValue> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
                }
                return m_variables;
            }
        }

        public TimelineRecord Clone()
        {
            return new TimelineRecord(this);
        }

        [DataMember(Name = "Issues", EmitDefaultValue = false, Order = 60)]
        private List<Issue> m_issues;

        [DataMember(Name = "Variables", EmitDefaultValue = false, Order = 80)]
        private Dictionary<String, VariableValue> m_variables;

        [DataMember(Name = "PreviousAttempts", EmitDefaultValue = false, Order = 120)]
        private List<TimelineAttempt> m_previousAttempts;
    }
}
