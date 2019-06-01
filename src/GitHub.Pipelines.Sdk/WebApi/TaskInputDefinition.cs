using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;
using CommonContracts = Microsoft.TeamFoundation.DistributedTask.Common.Contracts;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
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
