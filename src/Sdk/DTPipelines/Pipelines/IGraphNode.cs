using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphNode
    {
        String Name
        {
            get;
            set;
        }

        String DisplayName
        {
            get;
            set;
        }

        String Condition
        {
            get;
            set;
        }

        ISet<String> DependsOn
        {
            get;
        }

        void Validate(PipelineBuildContext context, ValidationResult result);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphNodeInstance
    {
        Int32 Attempt { get; set; }
        String Identifier { get; set; }
        String Name { get; set; }
        DateTime? StartTime { get; set; }
        DateTime? FinishTime { get; set; }
        TaskResult? Result { get; set; }
        Boolean SecretsAccessed { get; }
        IDictionary<String, VariableValue> Outputs { get; }
        void ResetSecretsAccessed();
    }
}
