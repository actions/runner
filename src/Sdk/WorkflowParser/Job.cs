#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public sealed class Job : IJob
    {
        [DataMember(Order = 0, Name = "type", EmitDefaultValue = true)]
        public JobType Type
        {
            get
            {
                return JobType.Job;
            }
        }

        [DataMember(Order = 1, Name = "id", EmitDefaultValue = false)]
        public StringToken? Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [DataMember(Order = 2, Name = "name", EmitDefaultValue = false)]
        public ScalarToken? Name
        {
            get;
            set;
        }

        public IList<StringToken> Needs
        {
            get
            {
                if (m_needs == null)
                {
                    m_needs = new List<StringToken>();
                }
                return m_needs;
            }
        }
        [DataMember(Order = 3, Name = "needs", EmitDefaultValue = false)]
        private List<StringToken>? m_needs;

        [DataMember(Order = 4, Name = "if", EmitDefaultValue = false)]
        public BasicExpressionToken? If
        {
            get;
            set;
        }

        [DataMember(Order = 5, Name = "strategy", EmitDefaultValue = false)]
        public TemplateToken? Strategy
        {
            get;
            set;
        }

        [DataMember(Order = 6, Name = "continue-on-error", EmitDefaultValue = false)]
        public ScalarToken? ContinueOnError
        {
            get;
            set;
        }

        [DataMember(Order = 7, Name = "timeout-minutes", EmitDefaultValue = false)]
        public ScalarToken? TimeoutMinutes
        {
            get;
            set;
        }

        [DataMember(Order = 8, Name = "cancel-timeout-minutes", EmitDefaultValue = false)]
        public ScalarToken? CancelTimeoutMinutes
        {
            get;
            set;
        }

        [DataMember(Order = 9, Name = "concurrency", EmitDefaultValue = false)]
        public TemplateToken? Concurrency
        {
            get;
            set;
        }

        [DataMember(Order = 10, Name = "permissions", EmitDefaultValue = false)]
        public Permissions? Permissions
        {
            get;
            set;
        }

        [DataMember(Order = 11, Name = "env", EmitDefaultValue = false)]
        public TemplateToken? Env
        {
            get;
            set;
        }

        [DataMember(Order = 12, Name = "environment", EmitDefaultValue = false)]
        public TemplateToken? Environment
        {
            get;
            set;
        }

        [DataMember(Order = 13, Name = "defaults", EmitDefaultValue = false)]
        public TemplateToken? Defaults
        {
            get;
            set;
        }

        [DataMember(Order = 14, Name = "runs-on", EmitDefaultValue = false)]
        public TemplateToken? RunsOn
        {
            get;
            set;
        }

        [DataMember(Order = 15, Name = "container", EmitDefaultValue = false)]
        public TemplateToken? Container
        {
            get;
            set;
        }

        [DataMember(Order = 16, Name = "services", EmitDefaultValue = false)]
        public TemplateToken? Services
        {
            get;
            set;
        }

        [DataMember(Order = 17, Name = "outputs", EmitDefaultValue = false)]
        public TemplateToken? Outputs
        {
            get;
            set;
        }

        public IList<IStep> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<IStep>();
                }
                return m_steps;
            }
        }
        [DataMember(Order = 18, Name = "steps", EmitDefaultValue = false)]
        private List<IStep>? m_steps;

        [DataMember(Order = 19, Name = "snapshot", EmitDefaultValue = false)]
        public TemplateToken? Snapshot
        {
            get;
            set;
        }

        public IJob Clone(bool omitSource)
        {
            var result = new Job
            {
                CancelTimeoutMinutes = CancelTimeoutMinutes?.Clone(omitSource) as ScalarToken,
                Concurrency = Concurrency?.Clone(omitSource),
                Container = Container?.Clone(omitSource),
                ContinueOnError = ContinueOnError?.Clone(omitSource) as ScalarToken,
                Defaults = Defaults?.Clone(omitSource),
                Env = Env?.Clone(omitSource),
                Environment = Environment?.Clone(omitSource),
                Id = Id?.Clone(omitSource) as StringToken,
                If = If?.Clone(omitSource) as BasicExpressionToken,
                Name = Name?.Clone(omitSource) as ScalarToken,
                Outputs = Outputs?.Clone(omitSource),
                Permissions = Permissions?.Clone(),
                RunsOn = RunsOn?.Clone(omitSource),
                Services = Services?.Clone(omitSource),
                Strategy = Strategy?.Clone(omitSource),
                TimeoutMinutes = TimeoutMinutes?.Clone(omitSource) as ScalarToken,
                Snapshot = Snapshot?.Clone(omitSource),
            };
            result.Needs.AddRange(Needs.Select(x => (x.Clone(omitSource) as StringToken)!));
            result.Steps.AddRange(Steps.Select(x => x.Clone(omitSource)));
            return result;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_needs?.Count == 0)
            {
                m_needs = null;
            }

            if (m_steps?.Count == 0)
            {
                m_steps = null;
            }
        }
    }
}
