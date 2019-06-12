using GitHub.Services.Common;
using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskLog : TaskLogReference
    {
        internal TaskLog()
        {
        }

        public TaskLog(String path)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(path, "path");
            this.Path = path;
        }

        [DataMember(EmitDefaultValue = false)]
        public Uri IndexLocation
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Path
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int64 LineCount
        {
            get;
            set;
        }

        [DataMember]
        public DateTime CreatedOn
        {
            get;
            internal set;
        }

        [DataMember]
        public DateTime LastChangedOn
        {
            get;
            internal set;
        }
    }
}
