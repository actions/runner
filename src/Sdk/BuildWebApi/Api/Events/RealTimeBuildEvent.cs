using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    public abstract class RealtimeBuildEvent
    {
        protected RealtimeBuildEvent(Int32 buildId)
        {
            this.BuildId = buildId;
        }

        [DataMember(IsRequired = true)]
        public Int32 BuildId
        {
            get;
            private set;
        }
    }
}
