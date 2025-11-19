#nullable enable

using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public sealed class RunStep : IStep
    {
        [DataMember(Order = 0, Name = "id", EmitDefaultValue = false)]
        public string? Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [DataMember(Order = 1, Name = "name", EmitDefaultValue = false)]
        public ScalarToken? Name
        {
            get;
            set;
        }

        [DataMember(Order = 2, Name = "if", EmitDefaultValue = false)]
        public BasicExpressionToken? If
        {
            get;
            set;
        }

        [DataMember(Order = 3, Name = "continue-on-error", EmitDefaultValue = false)]
        public ScalarToken? ContinueOnError
        {
            get;
            set;
        }

        [DataMember(Order = 4, Name = "timeout-minutes", EmitDefaultValue = false)]
        public ScalarToken? TimeoutMinutes
        {
            get;
            set;
        }

        [DataMember(Order = 5, Name = "env", EmitDefaultValue = false)]
        public TemplateToken? Env
        {
            get;
            set;
        }

        [DataMember(Order = 6, Name = "working-directory", EmitDefaultValue = false)]
        public ScalarToken? WorkingDirectory
        {
            get;
            set;
        }

        [DataMember(Order = 7, Name = "shell", EmitDefaultValue = false)]
        public ScalarToken? Shell
        {
            get;
            set;
        }

        [DataMember(Order = 8, Name = "run", EmitDefaultValue = false)]
        public ScalarToken? Run
        {
            get;
            set;
        }

        public IStep Clone(bool omitSource)
        {
            return new RunStep
            {
                ContinueOnError = ContinueOnError?.Clone(omitSource) as ScalarToken,
                Env = Env?.Clone(omitSource),
                Id = Id,
                If = If?.Clone(omitSource) as BasicExpressionToken,
                Name = Name?.Clone(omitSource) as ScalarToken,
                Run = Run?.Clone(omitSource) as ScalarToken,
                Shell = Shell?.Clone(omitSource) as ScalarToken,
                TimeoutMinutes = TimeoutMinutes?.Clone(omitSource) as ScalarToken,
                WorkingDirectory = WorkingDirectory?.Clone(omitSource) as ScalarToken,
            };
        }
    }
}
