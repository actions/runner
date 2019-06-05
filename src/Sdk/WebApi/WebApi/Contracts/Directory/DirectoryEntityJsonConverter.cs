using System;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.Directories
{
    internal class DirectoryEntityJsonConverter : VssSecureJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IDirectoryEntity);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonEntity = JToken.ReadFrom(reader);

            if (jsonEntity.Type == JTokenType.Null)
            {
                return null;
            }

            var entityType = jsonEntity.Value<string>(EntityTypePropertyName); 

            switch (entityType)
            {
                case DirectoryEntityType.User:
                    return jsonEntity.ToObject<DirectoryUser>();
                case DirectoryEntityType.Group:
                    return jsonEntity.ToObject<DirectoryGroup>();
                default:
                    throw new DirectoryEntityTypeException($"Cannot create IDirectoryEntity with entity type '{entityType}'");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            serializer.Serialize(writer, value);
        }

        private const string EntityTypePropertyName = "entityType";
    }
}
