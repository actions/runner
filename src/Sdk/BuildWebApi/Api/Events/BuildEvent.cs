using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public sealed class BuildEvent
    {
        public BuildEvent(
            String identifier)
            : this(identifier, (String)null)
        {
        }

        public BuildEvent(
            String identifier,
            String data)
            : this(identifier, new[] { data })
        {
            Identifier = identifier;
            if (data != null)
            {
                Data.Add(data);
            }
        }

        public BuildEvent(
            String identifier,
            IList<String> data)
        {
            Identifier = identifier;
            if (data != null && data.Count > 0)
            {
                Data.AddRange(data);
            }
        }

        [DataMember]
        public String Identifier
        {
            get;
            private set;
        }

        public IList<String> Data
        {
            get
            {
                if (m_data == null)
                {
                    m_data = new List<String>();
                }
                return m_data;
            }
        }

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        private List<String> m_data;
    }
}
