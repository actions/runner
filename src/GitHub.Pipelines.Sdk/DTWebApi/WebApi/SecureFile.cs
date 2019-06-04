using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class SecureFile
    {
        public SecureFile()
        {
        }

        private SecureFile(SecureFile secureFile, Boolean shallow = false)
        {
            this.Id = secureFile.Id;
            this.Name = secureFile.Name;
            this.Ticket = secureFile.Ticket;

            if (!shallow)
            {
                this.Properties = secureFile.Properties;
                this.CreatedBy = secureFile.CreatedBy;
                this.CreatedOn = secureFile.CreatedOn;
                this.ModifiedBy = secureFile.ModifiedBy;
                this.ModifiedOn = secureFile.ModifiedOn;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        public IDictionary<String, String> Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                return m_properties;
            }
            set
            {
                m_properties = value;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime CreatedOn
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public IdentityRef ModifiedBy
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime ModifiedOn
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Ticket
        {
            get;
            set;
        }

        public SecureFile Clone()
        {
            return new SecureFile(this);
        }

        public SecureFile CloneShallow()
        {
            return new SecureFile(this, true);
        }

        [DataMember(EmitDefaultValue = false, Name = "Properties")]
        private IDictionary<String, String> m_properties;
    }
}
