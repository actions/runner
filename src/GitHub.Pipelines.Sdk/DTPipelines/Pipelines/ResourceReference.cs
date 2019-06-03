using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a base set of properties common to all pipeline resource types.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ResourceReference
    {
        protected ResourceReference()
        {
        }

        protected ResourceReference(ResourceReference referenceToCopy)
        {
            this.Name = referenceToCopy.Name;
        }

        /// <summary>
        /// Gets or sets the name of the referenced resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<String>))]
        public ExpressionValue<String> Name
        {
            get;
            set;
        }

        public override String ToString()
        {
            var name = this.Name;
            if (name != null)
            {
                var s = name.Literal;
                if (!String.IsNullOrEmpty(s))
                {
                    return s;
                }
                
                s = name.Expression;
                if (!String.IsNullOrEmpty(s))
                {
                    return s;
                }
            }

            return null;
        }
    }
}
