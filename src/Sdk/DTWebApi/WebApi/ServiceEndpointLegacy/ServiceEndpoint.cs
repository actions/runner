using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using GitHub.Services.Common.Internal;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents an endpoint which may be used by an orchestration job.
    /// </summary>
    [DataContract]
    public class ServiceEndpoint
    {
        /// <summary>
        /// Constructs an <c>ServiceEndpoint</c> instance with empty values.
        /// </summary>
        public ServiceEndpoint()
        {
            m_data = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            IsReady = true;
        }

        private ServiceEndpoint(ServiceEndpoint endpointToClone)
        {
            Id = endpointToClone.Id;
            Name = endpointToClone.Name;
            Type = endpointToClone.Type;
            Url = endpointToClone.Url;
            Description = endpointToClone.Description;
            GroupScopeId = endpointToClone.GroupScopeId;

            if (endpointToClone.Authorization != null)
            {
                Authorization = endpointToClone.Authorization.Clone();
            }

            if (endpointToClone.m_data != null)
            {
                m_data = new Dictionary<String, String>(endpointToClone.m_data, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets the identifier of this endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the friendly name of the endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the type of the endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the owner of the endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the url of the endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Url
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the description of endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the authorization data for talking to the endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public EndpointAuthorization Authorization
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid GroupScopeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the custom data associated with this endpoint.
        /// </summary>
        public IDictionary<String, String> Data
        {
            get
            {
                return m_data;
            }

            set
            {
                if (value != null)
                {
                    m_data = new Dictionary<String, String>(value, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// Indicates whether service endpoint is shared with other projects or not. 
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean IsShared
        {
            get;
            set;
        }

        /// <summary>
        /// EndPoint state indictor
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        [JsonConverter(typeof(EndpointIsReadyConverter<bool>))]
        public bool IsReady
        {
            get;
            set;
        }

        /// <summary>
        /// Error message during creation/deletion of endpoint
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public JObject OperationStatus
        {
            get;
            set;
        }

        /// <summary>
        /// Performs a deep clone of the <c>ServiceEndpoint</c> instance.
        /// </summary>
        /// <returns>A new <c>ServiceEndpoint</c> instance identical to the current instance</returns>
        public ServiceEndpoint Clone()
        {
            return new ServiceEndpoint(this);
        }

        [DataMember(EmitDefaultValue = false, Name = "Data")]
        private Dictionary<String, String> m_data;
    }

    internal class EndpointIsReadyConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // we are converting every non-assignable thing to true
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Integer)
            {
                return serializer.Deserialize<T>(reader);
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var s = (string)reader.Value;

                if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s.Equals("0", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((bool)value ? true : false);
        }
    }
}
