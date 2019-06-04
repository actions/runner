using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using CommonContracts = GitHub.DistributedTask.Common.Contracts;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskInputDefinition : CommonContracts.TaskInputDefinitionBase
    {
        public TaskInputDefinition()
            : base()
        {
        }

        private TaskInputDefinition(TaskInputDefinition inputDefinitionToClone)
            : base(inputDefinitionToClone)
        {
        }

        private TaskInputDefinition(TaskInputDefinition inputDefinitionToClone, ISecuredObject securedObject)
            : base(inputDefinitionToClone, securedObject)
        {
        }

        public TaskInputDefinition Clone()
        {
            return new TaskInputDefinition(this);
        }

        public override CommonContracts.TaskInputDefinitionBase Clone(ISecuredObject securedObject)
        {
            return base.Clone(securedObject);
        }
    }
}
