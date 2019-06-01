using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// A variable group is a collection of related variables.
    /// </summary>
    [JsonConverter(typeof(VariableGroupJsonConverter))]
    [DataContract]
    public class VariableGroup
    {
        public VariableGroup()
        {
        }

        private VariableGroup(VariableGroup group)
        {
            this.Id = group.Id;
            this.Type = group.Type;
            this.Name = group.Name;
            this.Description = group.Description;
            this.ProviderData = group.ProviderData;
            this.CreatedBy = group.CreatedBy;
            this.CreatedOn = group.CreatedOn;
            this.ModifiedBy = group.ModifiedBy;
            this.ModifiedOn = group.ModifiedOn;
            this.IsShared = group.IsShared;
            this.Variables = group.Variables.ToDictionary(x => x.Key, x => x.Value.Clone());
        }

        /// <summary>
        /// Gets or sets id of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets type of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets name of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets description of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets provider data.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public VariableGroupProviderData ProviderData
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identity who created the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the time when variable group was created.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime CreatedOn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identity who modified the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef ModifiedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the time when variable group was modified
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime ModifiedOn
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether variable group is shared with other projects or not.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean IsShared
        {
            get;
            set;
        }

        public IDictionary<String, VariableValue> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
                }

                return m_variables;
            }
            set
            {
                if (value == null)
                {
                    m_variables = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    m_variables = new Dictionary<String, VariableValue>(value, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public VariableGroup Clone()
        {
            return new VariableGroup(this);
        }

        /// <summary>
        /// Gets or sets variables contained in the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Variables")]
        private Dictionary<String, VariableValue> m_variables;
    }

    internal sealed class VariableGroupJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanRead
        {
            get
            {
                return true;
            }
        }

        public override Boolean CanWrite
        {
            get
            {
                return false;
            }
        }

        public override Boolean CanConvert(Type objectType)
        {
            return typeof(VariableGroup).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            var variableGroupJsonObject = JObject.Load(reader);
            var variablesJsonObject = variableGroupJsonObject.GetValue("Variables", StringComparison.OrdinalIgnoreCase);
            var providerDataJsonObject = variableGroupJsonObject.GetValue("ProviderData", StringComparison.OrdinalIgnoreCase);

            String variablesJson = null;
            if (variablesJsonObject != null)
            {
                variablesJson = variablesJsonObject.ToString();
            }

            String providerDataJson = null;
            if (providerDataJsonObject != null)
            {
                providerDataJson = providerDataJsonObject.ToString();
            }

            VariableGroup variableGroup = new VariableGroup();
            using (var objectReader = variableGroupJsonObject.CreateReader())
            {
                serializer.Populate(objectReader, variableGroup);
            }

            if (String.IsNullOrEmpty(variableGroup.Type))
            {
                // To handle backward compat with clients making api calls without type
                variableGroup.Type = VariableGroupType.Vsts;
            }

            variableGroup.PopulateVariablesAndProviderData(variablesJson, providerDataJson);

            return variableGroup;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
