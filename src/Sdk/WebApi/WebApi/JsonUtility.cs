using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi
{
    public static class JsonUtility
    {
        public static JsonSerializer CreateJsonSerializer()
        {
            return JsonSerializer.Create(s_serializerSettings.Value);
        }

        public static T FromString<T>(String toDeserialize)
        {
            return FromString<T>(toDeserialize, s_serializerSettings.Value);
        }

        public static T FromString<T>(
            String toDeserialize,
            JsonSerializerSettings settings)
        {
            if (String.IsNullOrEmpty(toDeserialize))
            {
                return default(T);
            }

            using (StringReader sr = new StringReader(toDeserialize))
            using (JsonTextReader jsonReader = new JsonTextReader(sr))
            {
                JsonSerializer s = JsonSerializer.Create(settings);
                return s.Deserialize<T>(jsonReader);
            }
        }

        public static void Populate(
            String toDeserialize,
            Object target)
        {
            using (StringReader sr = new StringReader(toDeserialize))
            using (JsonTextReader jsonReader = new JsonTextReader(sr))
            {
                JsonSerializer s = JsonSerializer.Create(s_serializerSettings.Value);
                s.Populate(jsonReader, target);
            }
        }

        public static String ToString(Object toSerialize)
        {
            return ToString(toSerialize, false);
        }

        public static String ToString<T>(IList<T> toSerialize)
        {
            if (toSerialize == null || toSerialize.Count == 0)
            {
                return null;
            }

            return ToString(toSerialize, false);
        }

        public static String ToString(
            Object toSerialize, 
            Boolean indent)
        {
            if (toSerialize == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                JsonSerializer s = JsonSerializer.Create(indent ? s_indentSettings.Value : s_serializerSettings.Value);
                s.Serialize(jsonWriter, toSerialize);
            }

            return sb.ToString();
        }

        public static T Deserialize<T>(Stream streamToRead)
        {
            return Deserialize<T>(streamToRead, false);
        }

        public static T Deserialize<T>(
            Stream streamToRead,
            Boolean leaveOpen)
        {
            if (streamToRead == null)
            {
                return default(T);
            }

            using (StreamReader sr = new StreamReader(streamToRead, s_UTF8NoBOM, true, 80 * 1024, leaveOpen))
            using (JsonTextReader jsonReader = new JsonTextReader(sr))
            {
                JsonSerializer s = JsonSerializer.Create(s_serializerSettings.Value);
                return s.Deserialize<T>(jsonReader);
            }
        }


        public static T Deserialize<T>(Byte[] toDeserialize)
        {
            return Deserialize<T>(toDeserialize, s_serializerSettings.Value);
        }

        public static T Deserialize<T>(
            Byte[] toDeserialize,
            JsonSerializerSettings settings)
        {
            if (toDeserialize == null || toDeserialize.Length == 0)
            {
                return default(T);
            }

            using (MemoryStream ms = new MemoryStream(toDeserialize))
            {
                Stream streamToRead = ms;
                if (IsGZipStream(toDeserialize))
                {
                    streamToRead = new GZipStream(ms, CompressionMode.Decompress);
                }

                using (StreamReader sr = new StreamReader(streamToRead, s_UTF8NoBOM, true))
                using (JsonTextReader jsonReader = new JsonTextReader(sr))
                {
                    JsonSerializer s = JsonSerializer.Create(settings);
                    return s.Deserialize<T>(jsonReader);
                }
            }
        }

        public static JToken Map(
            this JToken token,
            Dictionary<JTokenType, Func<JToken, JToken>> mapFuncs)
        {
            // no map funcs, just clones
            mapFuncs = mapFuncs ?? new Dictionary<JTokenType, Func<JToken, JToken>>();
            
            Func<JToken, JToken> mapperFunc;
            
            // process token
            switch (token.Type)
            {
                case JTokenType.Array:
                    JArray newArray = new JArray();
                    foreach (JToken item in token.Children())
                    {
                        JToken child = item;
                        if (child.HasValues)
                        {
                            child = child.Map(mapFuncs);
                        }

                        if (mapFuncs.TryGetValue(child.Type, out mapperFunc))
                        {
                            child = mapperFunc(child);
                        }

                        newArray.Add(child);
                    }

                    return newArray;

                case JTokenType.Object:
                    JObject copy = new JObject();
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        JToken child = prop.Value;
                        if (child.HasValues)
                        {
                            child = child.Map(mapFuncs);
                        }

                        if (mapFuncs.TryGetValue(child.Type, out mapperFunc))
                        {
                            child = mapperFunc(child);
                        }

                        copy.Add(prop.Name, child);
                    }

                    return copy;
                
                case JTokenType.String:
                    if (mapFuncs.TryGetValue(JTokenType.String, out mapperFunc))
                    {
                        return mapperFunc(token);
                    }

                    return token;
                
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Guid:
                    return token;

                default:
                    throw new NotSupportedException(WebApiResources.UnexpectedTokenType());
            }
        }

        public static Byte[] Serialize(
            Object toSerialize,
            Boolean compress = true)
        {
            return Serialize(toSerialize, compress, s_UTF8NoBOM);
        }

        public static Byte[] Serialize(
            Object toSerialize,
            Boolean compress,
            Encoding encoding)
        {
            if (toSerialize == null)
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Stream streamToWrite = ms;
                if (compress)
                {
                    streamToWrite = new GZipStream(ms, CompressionMode.Compress);
                }

                using (StreamWriter sw = new StreamWriter(streamToWrite, encoding ?? s_UTF8NoBOM))
                using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
                {
                    JsonSerializer s = JsonSerializer.Create(s_serializerSettings.Value);
                    s.Serialize(jsonWriter, toSerialize);
                }

                return ms.ToArray();
            }
        }

        private static Boolean IsGZipStream(Byte[] data)
        {
            return data != null && data.Length > FullGzipHeaderLength && data[0] == GzipHeader[0] && data[1] == GzipHeader[1];
        }

        private const Int32 FullGzipHeaderLength = 10;
        private static readonly Byte[] GzipHeader = { 0x1F, 0x8B };
        private static readonly Encoding s_UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly Lazy<JsonSerializerSettings> s_serializerSettings = new Lazy<JsonSerializerSettings>(() => new VssJsonMediaTypeFormatter().SerializerSettings);
        private static readonly Lazy<JsonSerializerSettings> s_indentSettings = new Lazy<JsonSerializerSettings>(() => { var s = new VssJsonMediaTypeFormatter().SerializerSettings; s.Formatting = Formatting.Indented; return s; });
    }
}
