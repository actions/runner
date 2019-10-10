using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents information about resources used by builds in the system.
    /// </summary>
    [DataContract]
    public sealed class BuildResourceUsage
    {
        internal BuildResourceUsage()
        {
        }

        internal BuildResourceUsage(Int32 xaml, Int32 dtAgents, Int32 paidAgentSlots, Boolean isThrottlingEnabled = false)
        {
            this.XamlControllers = xaml;
            this.DistributedTaskAgents = dtAgents;
            this.TotalUsage = this.XamlControllers + (isThrottlingEnabled ? 0 : this.DistributedTaskAgents);
            this.PaidPrivateAgentSlots = paidAgentSlots;
        }

        /// <summary>
        /// The number of XAML controllers.
        /// </summary>
        [DataMember]
        public Int32 XamlControllers
        {
            get;
            internal set;
        }

        /// <summary>
        /// The number of build agents.
        /// </summary>
        [DataMember]
        public Int32 DistributedTaskAgents
        {
            get;
            internal set;
        }

        /// <summary>
        /// The total usage.
        /// </summary>
        [DataMember]
        public Int32 TotalUsage
        {
            get;
            internal set;
        }

        /// <summary>
        /// The number of paid private agent slots.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 PaidPrivateAgentSlots
        {
            get;
            internal set;
        }
    }
}
