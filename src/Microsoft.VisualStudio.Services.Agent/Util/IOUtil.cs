using Microsoft.VisualStudio.Services.Agent;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class IOUtil
    {
        public static void SaveObject(Object obj, string path)
        {
            string json = JsonConvert.SerializeObject(
                obj,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(path, json);
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
            // TODO: IO can be avoided here by using Path.GetDirectoryName.
            return new DirectoryInfo(currentAssemblyLocation).Parent.FullName;
        }

        public static string GetDiagPath()
        {
            return Path.Combine(new DirectoryInfo(GetBinPath()).Parent.FullName, "_diag");
        }

        public static string GetRootPath()
        {
            return new DirectoryInfo(GetBinPath()).Parent.FullName;
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(GetRootPath(), ".Agent");
        }

        public static string GetCredFilePath()
        {
            return Path.Combine(GetRootPath(), ".Credentials");
        }

        public static string GetWorkPath(IHostContext hostContext)
        {
            var configurationStore = hostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configurationStore.GetSettings();
            return Path.Combine(
                Path.GetDirectoryName(GetBinPath()),
                settings.WorkFolder);
        }

        public static string GetTasksPath(IHostContext hostContext)
        {
            return Path.Combine(GetWorkPath(hostContext), "_tasks");
        }        
    }
}