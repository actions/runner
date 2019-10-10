using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ContextScope
    {
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        [IgnoreDataMember]
        public String ContextName
        {
            get
            {
                var index = Name.LastIndexOf('.');
                if (index >= 0)
                {
                    return Name.Substring(index + 1);
                }

                return Name;
            }
        }

        [IgnoreDataMember]
        public String ParentName
        {
            get
            {
                var index = Name.LastIndexOf('.');
                if (index >= 0)
                {
                    return Name.Substring(0, index);
                }

                return String.Empty;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Inputs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TemplateToken Outputs { get; set; }
    }
}
