using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Actions.Pipelines.WebApi
{
    public class UnknownEnumJsonConverter : StringEnumConverter
    {
        public UnknownEnumJsonConverter()
        {
            this.CamelCaseText = true;
        }

        public override bool CanConvert(Type objectType)
        {
            // we require one member to be named "Unknown"
            return objectType.IsEnum && Enum.GetNames(objectType).Any(name => string.Equals(name, UnknownName, StringComparison.OrdinalIgnoreCase));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Newtonsoft doesn't call CanConvert if you specify the converter using a JsonConverter attribute
            // they just assume you know what you're doing :)
            if (!CanConvert(objectType))
            {
                // if there's no Unknown value, fall back to the StringEnumConverter behavior
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                var intValue = Convert.ToInt32(reader.Value);
                var values = (int[])Enum.GetValues(objectType);
                if (values.Contains(intValue))
                {
                    return Enum.Parse(objectType, intValue.ToString());
                }
            }

            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value.ToString();
                return UnknownEnum.Parse(objectType, stringValue);
            }

            // we know there's an Unknown value because CanConvert returned true
            return Enum.Parse(objectType, UnknownName);
        }

        private const string UnknownName = "Unknown";
    }
}
