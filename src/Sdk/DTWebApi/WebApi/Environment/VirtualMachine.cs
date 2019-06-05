using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class VirtualMachine
    {
        [DataMember]
        public Int32 Id { get; set; }

        [DataMember]
        public TaskAgent Agent { get; set; }

        /// <summary>
        /// List of tags
        /// </summary>
        public IList<String> Tags
        {
            get
            {
                if (this.tags == null)
                {
                    this.tags = new List<String>();
                }

                return this.tags;
            }
            set
            {
                this.tags = value;
            }
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Tags")]
        private IList<String> tags;
    }
}
