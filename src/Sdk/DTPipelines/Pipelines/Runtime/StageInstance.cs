using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class StageInstance : GraphNodeInstance<Stage>
    {
        public StageInstance()
        {
        }

        public StageInstance(String name)
            : this(name, TaskResult.Succeeded)
        {
        }

        public StageInstance(
            String name,
            Int32 attempt)
            : this(name, attempt, null, TaskResult.Succeeded)
        {
        }

        public StageInstance(Stage stage)
            : this(stage, 1)
        {
        }

        public StageInstance(
            Stage stage,
            Int32 attempt)
            : this(stage.Name, attempt, stage, TaskResult.Succeeded)
        {
        }

        public StageInstance(
            String name,
            TaskResult result)
            : this(name, 1, null, result)
        {
        }

        public StageInstance(
            String name,
            Int32 attempt,
            Stage definition,
            TaskResult result)
            : base(name, attempt, definition, result)
        {
        }

        public static implicit operator StageInstance(String name)
        {
            return new StageInstance(name);
        }
    }
}
