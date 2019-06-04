using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Services.WebApi
{
    public abstract class VssSecureJsonConverter : JsonConverter
    {
        public override abstract bool CanConvert(Type objectType);

        public override abstract object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Validate(value, serializer);
        }

        private void Validate(object value, JsonSerializer serializer)
        {
            VssSecureJsonConverterHelper.Validate?.Invoke(value, serializer);
        }
    }

    public abstract class VssSecureCustomCreationConverter<T> : CustomCreationConverter<T>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Validate(value, serializer);
        }

        private void Validate(object value, JsonSerializer serializer)
        {
            VssSecureJsonConverterHelper.Validate?.Invoke(value, serializer);
        }
    }

    public abstract class VssSecureDateTimeConverterBase : DateTimeConverterBase
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Validate(value, serializer);
        }

        private void Validate(object value, JsonSerializer serializer)
        {
            VssSecureJsonConverterHelper.Validate?.Invoke(value, serializer);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class VssSecureJsonConverterHelper
    {
        /// <summary>
        /// The action to validate the object being converted.
        /// </summary>
        public static Action<object, JsonSerializer> Validate { get; set; }
    }
}
