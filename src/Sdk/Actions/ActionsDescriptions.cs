using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;

namespace Sdk.Actions {

    public class ActionsDescriptions
    {

        public static Dictionary<string, TSource> ToOrdinalIgnoreCaseDictionary<TSource>(IEnumerable<KeyValuePair<string, TSource>> source) {
            var ret = new Dictionary<string, TSource>(StringComparer.OrdinalIgnoreCase);
            foreach(var kv in source) {
                ret[kv.Key] = kv.Value;
            }
            return ret;
        }

        public string Description { get; set; }

        public Dictionary<string, string> Versions { get; set; }
        public static Dictionary<string, Dictionary<string, ActionsDescriptions>> LoadDescriptions() {
            var assembly = Assembly.GetExecutingAssembly();
            var json = default(String);
            using (var stream = assembly.GetManifestResourceStream("descriptions.json"))
            using (var streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            return ToOrdinalIgnoreCaseDictionary(JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, ActionsDescriptions>>>(json).Select(kv => new KeyValuePair<string, Dictionary<string, ActionsDescriptions>>(kv.Key, ToOrdinalIgnoreCaseDictionary(kv.Value))));
        }
    }

}