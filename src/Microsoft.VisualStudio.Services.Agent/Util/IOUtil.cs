using Microsoft.VisualStudio.Services.Agent;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

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
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static string GetDiagPath()
        {
            return Path.Combine(
                Path.GetDirectoryName(GetBinPath()),
                Constants.Path.DiagDirectory);
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
            return Path.Combine(
                GetWorkPath(hostContext),
                Constants.Path.TasksDirectory);
        }
    }
}