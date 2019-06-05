using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using CommonContracts = GitHub.DistributedTask.Common.Contracts;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskSourceDefinition : CommonContracts.TaskSourceDefinitionBase
    {
        public TaskSourceDefinition()
            : base()
        {
        }

        private TaskSourceDefinition(TaskSourceDefinition inputDefinitionToClone)
            : base(inputDefinitionToClone)
        {
        }

        private TaskSourceDefinition(TaskSourceDefinition inputDefinitionToClone, ISecuredObject securedObject)
            : base(inputDefinitionToClone, securedObject)
        {
        }

        public TaskSourceDefinition Clone()
        {
            return new TaskSourceDefinition(this);
        }

        public override CommonContracts.TaskSourceDefinitionBase Clone(ISecuredObject securedObject)
        {
            return new TaskSourceDefinition(this, securedObject);
        }
    }
}
