using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhaseDependency
    {
        [JsonConstructor]
        public PhaseDependency()
        {
        }

        private PhaseDependency(PhaseDependency dependencyToCopy)
        {
            this.Scope = dependencyToCopy.Scope;
            this.Event = dependencyToCopy.Event;
        }

        [DataMember]
        public String Scope
        {
            get;
            set;
        }

        [DataMember]
        public String Event
        {
            get;
            set;
        }

        /// <summary>
        /// Implicitly converts a <c>Phase</c> to a <c>PhaseDependency</c> to enable easier modeling of graphs.
        /// </summary>
        /// <param name="dependency">The phase which should be converted to a dependency</param>
        public static implicit operator PhaseDependency(Phase dependency)
        {
            return PhaseCompleted(dependency.Name);
        }

        public static PhaseDependency PhaseCompleted(String name)
        {
            return new PhaseDependency { Scope = name, Event = "Completed" };
        }

        internal PhaseDependency Clone()
        {
            return new PhaseDependency(this);
        }
    }
}
