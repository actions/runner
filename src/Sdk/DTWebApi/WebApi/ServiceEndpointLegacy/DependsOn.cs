namespace GitHub.DistributedTask.WebApi
{
    using System;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    [DataContract]
    public class DependsOn
    {
        [DataMember(EmitDefaultValue = false)]
        public string Input { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<DependencyBinding> Map { get; set; }
    }

    public class DependencyBinding
    {
        [DataMember(EmitDefaultValue = false)]
        public string Key;

        [DataMember(EmitDefaultValue = false)]
        public string Value;
    }
}
