using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Common.Contracts
{
    [DataContract]
    public class TaskSourceDefinitionBase : BaseSecuredObject
    {
        public TaskSourceDefinitionBase()
        {
            AuthKey = String.Empty;
            Endpoint = String.Empty;
            Selector = String.Empty;
            Target = String.Empty;
            KeySelector = String.Empty;
        }

        protected TaskSourceDefinitionBase(TaskSourceDefinitionBase inputDefinitionToClone)
            : this(inputDefinitionToClone, null)
        {
        }

        protected TaskSourceDefinitionBase(TaskSourceDefinitionBase inputDefinitionToClone, ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Endpoint = inputDefinitionToClone.Endpoint;
            this.Target = inputDefinitionToClone.Target;
            this.AuthKey = inputDefinitionToClone.AuthKey;
            this.Selector = inputDefinitionToClone.Selector;
            this.KeySelector = inputDefinitionToClone.KeySelector;
        }

        public virtual TaskSourceDefinitionBase Clone(ISecuredObject securedObject)
        {
            return new TaskSourceDefinitionBase(this, securedObject);
        }

        [DataMember(EmitDefaultValue = false)]
        public String Endpoint
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Target
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String AuthKey
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Selector
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String KeySelector
        {
            get;
            set;
        }
    }
}
