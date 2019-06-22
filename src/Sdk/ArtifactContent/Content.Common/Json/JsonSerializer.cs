using System.Net.Http;
using Newtonsoft.Json;
using System.Collections;

namespace GitHub.Services.Content.Common
{
    public static class JsonSerializer
    {
        static JsonSerializer()
        {
            Settings = MakeDefaultSettings();

            Serializer = new Newtonsoft.Json.JsonSerializer()
            {
                DateTimeZoneHandling = Settings.DateTimeZoneHandling,
                DateFormatHandling = Settings.DateFormatHandling,
                DateParseHandling = Settings.DateParseHandling,
                EqualityComparer = Settings.EqualityComparer,
            };
        }

        public static JsonSerializerSettings MakeDefaultSettings() => new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,

            // We don't want *default* parsing of Dates. When we need it we'll
            // ask for it!
            DateParseHandling = DateParseHandling.None,

            // Gets or sets the equality comparer used by the serializer when comparing *references*.
            //
            // By default Newtonsoft.json uses Object.Equals. We replace this by Object.ReferenceEquals.
            //
            EqualityComparer = new ReferenceEqualityComparer()
        };

        public static readonly Newtonsoft.Json.JsonSerializer Serializer;

        private static readonly JsonSerializerSettings Settings;

        public static string Serialize<T>(T dataContractObject)
        {
            return Serialize(dataContractObject, Settings);
        }

        public static string Serialize<T>(T dataContractObject, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(dataContractObject, settings);
        }

        public static HttpContent SerializeToContent<T>(T dataContractObject)
        {
            return dataContractObject == null
                ? null
                : new StringContent(JsonConvert.SerializeObject(dataContractObject, Settings), StrictEncodingWithoutBOM.UTF8, "application/json");
        }

        public static T Deserialize<T>(string json)
        {
            return Deserialize<T>(json, Settings);
        }

        public static T Deserialize<T>(string json, JsonSerializerSettings settings)
        {
            if (json.Equals(string.Empty))
            {
                throw new JsonReaderException("The empty string isn't valid JSON");
            }
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static T Deserialize<T>(System.IO.Stream stream)
        {
            using (var sr = new System.IO.StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return Serializer.Deserialize<T>(jsonTextReader);
            }
        }

        private class ReferenceEqualityComparer : IEqualityComparer
        {
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => GetHashCode(obj);
        }
    }
}
