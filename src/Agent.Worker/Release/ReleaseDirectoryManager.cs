using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseDirectoryManager : AgentService, IReleaseDirectoryManager
    {
        public ReleaseDefinitionToFolderMap PrepareArtifactsDirectory(
            string workingDirectory,
            string collectionId,
            string projectId,
            string releaseDefinitionId)
        {
            Trace.Entering();
            ReleaseDefinitionToFolderMap map = null;
            string mapFile = Path.Combine(
                workingDirectory,
                Constants.Release.Path.ReleaseDirectoryPrefix,
                Constants.Release.Path.RootMappingDirectory,
                collectionId,
                projectId,
                releaseDefinitionId,
                Constants.Release.Path.DefinitionMapping);

            Trace.Verbose($"Mappings file: {mapFile}");
            map = LoadIfExists(mapFile);
            if (map == null)
            {
                Trace.Verbose("Mappings file does not exist. A new mapping file will be created");
                var folderNameToUse = ComputeFolderName(workingDirectory);
                map = new ReleaseDefinitionToFolderMap();
                map.ReleaseDirectory = folderNameToUse.ToString();
                WriteToFile(mapFile, map);
                Trace.Verbose($"Created a new mapping file: {mapFile}");
            }

            return map;
        }

        private int ComputeFolderName(string workingDirectory)
        {
            Trace.Entering();
            var releaseDirectory = Path.Combine(
                workingDirectory,
                Constants.Release.Path.ReleaseDirectoryPrefix);
            if (Directory.Exists(releaseDirectory))
            {
                Regex regex = new Regex(@"^[0-9]*$");
                var dirs = Directory.GetDirectories(releaseDirectory);
                var integerFolderNames = dirs.Select(Path.GetFileName).Where(name => regex.IsMatch(name));
                Trace.Verbose($"Number of folder with integer names: {integerFolderNames.Count()}");

                if (integerFolderNames.Any())
                {
                    var max = integerFolderNames.Select(Int32.Parse).Max();
                    return max + 1;
                }
            }

            return 1;
        }

        private ReleaseDefinitionToFolderMap LoadIfExists(string mappingFile)
        {
            Trace.Entering();
            Trace.Verbose($"Loading mapping file: {mappingFile}");
            if (!File.Exists(mappingFile))
            {
                return null;
            }

            string content = File.ReadAllText(mappingFile);
            var map = JsonConvert.DeserializeObject<ReleaseDefinitionToFolderMap>(content);
            return map;
        }

        private void WriteToFile(string file, object value)
        {
            Trace.Entering();
            Trace.Verbose($"Writing config to file: {file}");

            // Create the directory if it does not exist.
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            IOUtil.SaveObject(value, file);
        }
    }
}