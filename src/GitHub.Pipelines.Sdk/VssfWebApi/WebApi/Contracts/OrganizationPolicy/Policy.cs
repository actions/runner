﻿using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Organization.Client
{
    [DataContract]
    [ClientIncludeModel]
    public sealed class Policy
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public object Value { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public object EffectiveValue { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool Enforce { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsValueUndefined { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Policy ParentPolicy { get; set; }
    }
}
