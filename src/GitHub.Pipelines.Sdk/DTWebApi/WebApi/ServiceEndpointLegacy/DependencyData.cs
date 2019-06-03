namespace GitHub.DistributedTask.WebApi
{
    using System;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    [DataContract]
    public class DependencyData
    {
        [DataMember(EmitDefaultValue = false)]
        public String Input { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<KeyValuePair<String, List<KeyValuePair<String, String>>>> Map;
    }
}
