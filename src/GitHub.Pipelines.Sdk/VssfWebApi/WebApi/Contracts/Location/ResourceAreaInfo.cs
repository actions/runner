using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Location
{
    [DataContract]
    public class ResourceAreaInfo
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String LocationUrl
        {
            get;
            set;
        }
    }
}
