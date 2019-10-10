using System;
using Newtonsoft.Json.Serialization;

namespace GitHub.Services.WebApi
{
    internal class VssCamelCasePropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type type)
        {
            // We need to preserve case for keys in the PropertiesCollection
            JsonDictionaryContract contract = base.CreateDictionaryContract(type);
            contract.DictionaryKeyResolver = (name) => name;
            return contract;
        }
    }

    internal class VssCamelCasePropertyNamesPreserveEnumsContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type type)
        {
            // We need to preserve case for keys in the PropertiesCollection and optionally use integer values for enum keys
            JsonDictionaryContract contract = base.CreateDictionaryContract(type);

            Type keyType = contract.DictionaryKeyType;
            Boolean isEnumKey = keyType != null ? keyType.IsEnum : false;

            if (isEnumKey)
            {
                contract.DictionaryKeyResolver = (name) => ((int)Enum.Parse(keyType, name)).ToString();
            }
            else
            {
                contract.DictionaryKeyResolver = (name) => name;
            }

            return contract;
        }
    }
}
