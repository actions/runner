using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.FormInput;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentCloudType
    {

        public TaskAgentCloudType()
        {
        }

        /// <summary>
        /// Gets or sets the name of agent cloud type.
        /// </summary>
        [DataMember]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the display name of agnet cloud type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName { get; set; }

        public List<InputDescriptor> InputDescriptors
        {
            get
            {
                return m_inputDescriptors ?? (m_inputDescriptors = new List<InputDescriptor>());
            }

            set
            {
                m_inputDescriptors = value;
            }
        }

        /// <summary>
        /// Gets or sets the input descriptors 
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "InputDescriptors")]
        private List<InputDescriptor> m_inputDescriptors;
    }
}
