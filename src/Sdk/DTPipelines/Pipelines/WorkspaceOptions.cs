using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class WorkspaceOptions
    {
        public WorkspaceOptions()
        {
        }

        private WorkspaceOptions(WorkspaceOptions optionsToCopy)
        {
            this.Clean = optionsToCopy.Clean;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Clean
        {
            get;
            set;
        }

        public WorkspaceOptions Clone()
        {
            return new WorkspaceOptions(this);
        }
    }
}
