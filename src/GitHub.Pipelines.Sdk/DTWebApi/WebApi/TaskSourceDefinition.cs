using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;
using CommonContracts = Microsoft.TeamFoundation.DistributedTask.Common.Contracts;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
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
