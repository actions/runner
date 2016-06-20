using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Agent.Worker.Common
{
    public static class FileSystemHelper
    {
        public static void WriteJsonSerializeToFile(string file, object value)
        {
            // Create the directory if it does not exist.
            Directory.CreateDirectory(Path.GetDirectoryName(file));

            // Serialize the object.
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters.Add(new StringEnumConverter() { CamelCaseText = true });
            string contents = JsonConvert.SerializeObject(
                value: value,
                formatting: Formatting.Indented,
                settings: settings);

            // Lock write access on the file so the garbage collector service
            // can also lock on write access to safely handle the file.
            using (FileStream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(contents);
                    writer.Flush();
                    stream.Flush();
                }
            }
        }
    }
}
