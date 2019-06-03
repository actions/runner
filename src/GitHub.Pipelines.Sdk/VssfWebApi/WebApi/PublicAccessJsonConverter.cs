using System;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// A JsonConverter that sets the value of a property or field to its type's default value if the value is not null
    /// and the ShouldClearValueFunction property is set and the ShouldClearValueFunction function returns true.  This
    /// can only be used on properties and fields. The converter will fail if used on at the class level.
    /// </summary>
    /// <typeparam name="T">The type of the property or field.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DefaultValueOnPublicAccessJsonConverter<T> : PublicAccessJsonConverter<T>
    {
        public override object GetDefaultValue()
        {
            return default(T);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PublicAccessJsonConverter<T> : PublicAccessJsonConverter
    {
        public PublicAccessJsonConverter()
        {
            if (typeof(T) == typeof(bool))
            {
                // We are not supporting boolean types.  This is because the converter does not get invoked in case of default values,
                // therefore you can infer that a boolean type is false if the value does not exist in the json, and true if it does
                // exist in the json even though the value would be set to false.
                throw new ArgumentException($"The {nameof(PublicAccessJsonConverter<T>)} does not support Boolean types, because the value can be inferred from the existance or non existance of the property in the json.");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(T).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PublicAccessJsonConverter : JsonConverter
    {
        public abstract object GetDefaultValue();

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null && ShouldClearValueFunction != null && ShouldClearValueFunction())
            {
                serializer.Serialize(writer, GetDefaultValue());
            }
            else
            {
                // this is the default serialization.  This will fail if the converter is used at the class level rather than
                // at the member level, because the default serialization will reinvoke this converter resulting in an exception.
                serializer.Serialize(writer, value);
            }
        }

        internal static Func<bool> ShouldClearValueFunction { get; set; }
    }
}
