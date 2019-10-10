using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    [JsonConverter(typeof(VariableGroupParametersJsonConverter))]
    [DataContract]
    public class VariableGroupParameters
    {
        /// <summary>
        /// Sets type of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// Sets name of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Sets description of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// Sets provider data.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public VariableGroupProviderData ProviderData
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

        /// <summary>
        /// Sets variables contained in the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Variables")]
        private Dictionary<String, VariableValue> m_variables;
    }

    internal sealed class VariableGroupParametersJsonConverter : VssSecureJsonConverter
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
            return typeof(VariableGroupParameters).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            VariableGroupParameters variableGroupParameters = new VariableGroupParameters();
            using (var objectReader = variableGroupJsonObject.CreateReader())
            {
                serializer.Populate(objectReader, variableGroupParameters);
            }

            if (String.IsNullOrEmpty(variableGroupParameters.Type))
            {
                // To handle backward compat with clients making api calls without type
                variableGroupParameters.Type = VariableGroupType.Vsts;
            }

            variableGroupParameters.PopulateVariablesAndProviderData(variablesJson, providerDataJson);

            return variableGroupParameters;
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
