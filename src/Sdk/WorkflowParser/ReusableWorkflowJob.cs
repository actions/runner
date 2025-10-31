#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public sealed class ReusableWorkflowJob : IJob
    {
        [DataMember(Order = 0, Name = "type", EmitDefaultValue = false)]
        public JobType Type
        {
            get
            {
                return JobType.ReusableWorkflowJob;
            }
        }

        [DataMember(Order = 1, Name = "id", EmitDefaultValue = false)]
        public StringToken? Id
        {
            get;
            set;
        }

        [DataMember(Order = 2, Name = "name", EmitDefaultValue = false)]
        public ScalarToken? Name
        {
            get;
            set;
        }

        [IgnoreDataMember]
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

        [DataMember(Order = 4, Name = "if", EmitDefaultValue = false)]
        public BasicExpressionToken? If
        {
            get;
            set;
        }

        [DataMember(Order = 5, Name = "ref", EmitDefaultValue = false)]
        public StringToken? Ref
        {
            get;
            set;
        }

        [DataMember(Order = 6, Name = "permissions", EmitDefaultValue = false)]
        public Permissions? Permissions
        {
            get;
            set;
        }

        [DataMember(Order = 7, Name = "input-definitions", EmitDefaultValue = false)]
        public MappingToken? InputDefinitions
        {
            get;
            set;
        }

        [DataMember(Order = 8, Name = "input-values", EmitDefaultValue = false)]
        public MappingToken? InputValues
        {
            get;
            set;
        }

        [DataMember(Order = 9, Name = "secret-definitions", EmitDefaultValue = false)]
        public MappingToken? SecretDefinitions
        {
            get;
            set;
        }

        [DataMember(Order = 10, Name = "secret-values", EmitDefaultValue = false)]
        public MappingToken? SecretValues
        {
            get;
            set;
        }

        [DataMember(Order = 11, Name = "inherit-secrets", EmitDefaultValue = false)]
        public bool InheritSecrets
        {
            get;
            set;
        }

        [DataMember(Order = 12, Name = "outputs", EmitDefaultValue = false)]
        public MappingToken? Outputs
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

        [DataMember(Order = 14, Name = "env", EmitDefaultValue = false)]
        public TemplateToken? Env
        {
            get;
            set;
        }

        [DataMember(Order = 15, Name = "concurrency", EmitDefaultValue = false)]
        public TemplateToken? Concurrency
        {
            get;
            set;
        }

        [DataMember(Order = 16, Name = "embedded-concurrency", EmitDefaultValue = false)]
        public TemplateToken? EmbeddedConcurrency
        {
            get;
            set;
        }

        [DataMember(Order = 17, Name = "strategy", EmitDefaultValue = false)]
        public TemplateToken? Strategy
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public IList<IJob> Jobs
        {
            get
            {
                if (m_jobs == null)
                {
                    m_jobs = new List<IJob>();
                }
                return m_jobs;
            }
        }

        public IJob Clone(bool omitSource)
        {
            var result = new ReusableWorkflowJob
            {
                Concurrency = Concurrency?.Clone(omitSource),
                Defaults = Defaults?.Clone(omitSource),
                Name = Name?.Clone(omitSource) as ScalarToken,
                EmbeddedConcurrency = EmbeddedConcurrency?.Clone(omitSource),
                Env = Env?.Clone(omitSource),
                Id = Id?.Clone(omitSource) as StringToken,
                If = If?.Clone(omitSource) as BasicExpressionToken,
                InheritSecrets = InheritSecrets,
                InputDefinitions = InputDefinitions?.Clone(omitSource) as MappingToken,
                InputValues = InputValues?.Clone(omitSource) as MappingToken,
                Outputs = Outputs?.Clone(omitSource) as MappingToken,
                Permissions = Permissions?.Clone(),
                Ref = Ref?.Clone(omitSource) as StringToken,
                SecretDefinitions = SecretDefinitions?.Clone(omitSource) as MappingToken,
                SecretValues = SecretValues?.Clone(omitSource) as MappingToken,
                Strategy = Strategy?.Clone(omitSource),
            };
            result.Jobs.AddRange(Jobs.Select(x => x.Clone(omitSource)));
            result.Needs.AddRange(Needs.Select(x => (x.Clone(omitSource) as StringToken)!));
            return result;
        }

        [DataMember(Order = 3, Name = "needs", EmitDefaultValue = false)]
        private List<StringToken>? m_needs;

        [DataMember(Order = 18, Name = "jobs", EmitDefaultValue = false)]
        private List<IJob>? m_jobs;
    }
}
