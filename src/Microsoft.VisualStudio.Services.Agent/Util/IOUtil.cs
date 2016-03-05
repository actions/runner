using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class IOUtil
    {
        public static void SaveObject(Object obj, string path) 
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText (path, json);
        }

        public static T LoadObject<T>(string path)
        {
            string json = File.ReadAllText(path);
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }
        
        public static string GetBinPath()
        {
            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            return new DirectoryInfo(currentAssemblyLocation).Parent.FullName.ToString();         
        }
        
        public static string GetDiagPath()
        {
            return Path.Combine(new DirectoryInfo(GetBinPath()).Parent.FullName.ToString(), "_diag");         
        }

        public static string GetTasksPath()
        {
            return Path.Combine(new DirectoryInfo(GetBinPath()).Parent.FullName.ToString(), "_tasks");
        }
    }
}