using System;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Converts between a <see cref="License"/> and a JSON-serialized license string
    /// </summary>
    internal sealed class LicenseJsonConverter : VssSecureJsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <returns>true if this instance can convert the specified object type; otherwise, false.</returns>        
        public override bool CanConvert(Type objectType)
        {
            return typeof(License).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The Newtonsoft.Json.JsonReader to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return License.Parse(reader.Value.ToString(), ignoreCase: true);
            }

            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            throw new JsonSerializationException();
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The Newtonsoft.Json.JsonWriter to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var license = (License)value;
            writer.WriteValue(license.ToString());
        }
    }
}
